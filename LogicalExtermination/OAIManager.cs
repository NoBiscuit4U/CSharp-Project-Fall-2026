using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using IronPython.Runtime.Operations;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

public record StructuredResult(){
    public required string Problem {get;set;}=null!;
    public required List<string> Codeblocks {get;set;}=null!;
    public required string CompletedCode {get;set;}=null!;
    public required List<string> Variables {get;set;}=null!;
    public required string TerminalOutput {get;set;}=null!;
}
public record BlockVariableResponse(){
    public required List<string> CodeBlocks {get;set;}=null!;
    public required List<string> Variables {get;set;}=null!;
}
public record ProblemResponse(){
    public required string Problem {get;set;}=null!;
}

public record AnswerResponse(){
    public required string Answer {get;set;}=null!;
}

public delegate StructuredResult ProcessResponse(StructuredResult response);

namespace Backend{
    sealed class OAIManager{
        private static readonly HashSet<string> PythonKeywords=[
            "False","None","True","and","as","assert","async","await","break","class","continue",
            "def","del","elif","else","except","finally","for","from","global","if","import",
            "in","is","lambda","nonlocal","not","or","pass","raise","return","try","while",
            "with","yield"
        ];
        private static readonly HashSet<string> PythonBuiltins=[
            "__import__","abs","all","any","ascii","bin","bool","breakpoint","bytearray","bytes",
            "callable","chr","classmethod","compile","complex","delattr","dict","dir","divmod",
            "enumerate","eval","exec","filter","float","format","frozenset","getattr","globals",
            "hasattr","hash","help","hex","id","input","int","isinstance","issubclass","iter",
            "len","list","locals","map","max","memoryview","min","next","object","oct","open",
            "ord","pow","print","property","range","repr","reversed","round","set","setattr",
            "slice","sorted","staticmethod","str","sum","super","tuple","type","vars","zip",
            "ArithmeticError","AssertionError","AttributeError","BaseException","BlockingIOError",
            "BrokenPipeError","BufferError","BytesWarning","ChildProcessError","ConnectionError",
            "DeprecationWarning","EOFError","EnvironmentError","Exception","FileExistsError",
            "FileNotFoundError","FloatingPointError","FutureWarning","GeneratorExit","IOError",
            "ImportError","ImportWarning","IndentationError","IndexError","InterruptedError",
            "IsADirectoryError","KeyError","KeyboardInterrupt","LookupError","MemoryError",
            "ModuleNotFoundError","NameError","NotADirectoryError","NotImplementedError",
            "OSError","OverflowError","PendingDeprecationWarning","PermissionError","ProcessLookupError",
            "RecursionError","ReferenceError","ResourceWarning","RuntimeError","RuntimeWarning",
            "StopAsyncIteration","StopIteration","SyntaxError","SyntaxWarning","SystemError",
            "SystemExit","TabError","TimeoutError","TypeError","UnboundLocalError","UnicodeDecodeError",
            "UnicodeEncodeError","UnicodeError","UnicodeTranslateError","UnicodeWarning",
            "UserWarning","ValueError","Warning","ZeroDivisionError","Ellipsis","NotImplemented",
            "__debug__"
        ];

        private readonly ProcessResponse processResponse;
        private PythonRunner pythonRunner;

        private readonly AIAgent problem_agent;
        private readonly AIAgent answer_agent;

        public OAIManager(string Model,PythonRunner pythonRunner){
            this.pythonRunner=pythonRunner;

            DotNetEnv.Env.TraversePath().Load();
            string? apiKey=Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if(string.IsNullOrWhiteSpace(apiKey)){
                throw new InvalidOperationException("The OPENAI_API_KEY environment variable is not set.");
            }
            IChatClient chatClient=new ChatClient(Model,apiKey).AsIChatClient();

            answer_agent=chatClient.AsAIAgent(
                    instructions:Constants.OAIConstants.answer_agent_instructions,
                    name:"Answer Agent");
            problem_agent=chatClient.AsAIAgent(
                    instructions:Constants.OAIConstants.problem_agent_instructions,
                    name:"Problem Agent");

            processResponse=response =>{
                response.Problem ??= "Error Occurred";
                response.Codeblocks ??= new List<string> {"EMPTY"};
                response.CompletedCode ??= "Error Occurred";
                response.Variables ??=new List<string> {"EMPTY"};
                response.TerminalOutput ??= "Error Occurred";
                return response;
            };

            processResponse+=response =>{
                Random.Shared.Shuffle(CollectionsMarshal.AsSpan(response.Codeblocks));

                foreach(string block in response.Codeblocks){
                    if(block.strip()[0]=='#'){
                        response.Codeblocks.Remove(block);
                    }
                }
                return response;
            };
        }

        private string StringListToString(List<string> list){
            return string.Join(Environment.NewLine,list);
        }

//        private List<string> GetInputs(int num){
//            return Constants.OAIConstants.static_var_val_string
//                .OrderBy(_ => Random.Shared.Next())
//                .Take(num)
//                .ToList();
//        }

        private List<string> GetInputVariableNames(List<string> inputDefinitions){
            return inputDefinitions.Select(definition =>
            {
                Match match=Regex.Match(definition,@"^\s*([A-Za-z_]\w*)\s*=");
                if(!match.Success){
                    throw new InvalidOperationException($"Invalid input variable definition: {definition}");
                }

                return match.Groups[1].Value;
            }).Distinct(StringComparer.Ordinal).ToList();
        }

        private string BuildCompletedCode(List<string> inputDefinitions,string generatedCode,List<string> inputNames){
            string[] codeLines=generatedCode
                .Split(["\r\n","\n"],StringSplitOptions.None)
                .Where(line=>!inputNames.Any(name=>Regex.IsMatch(line,$@"^\s*{Regex.Escape(name)}\s*=")))
                .ToArray();

            return string.Join(Environment.NewLine,inputDefinitions.Concat(codeLines).Where(line=>!string.IsNullOrWhiteSpace(line)));
        }

        private BlockVariableResponse GenerateBlockVariableResponse(string completedCode,List<string> inputNames){
            string[] lines=completedCode.Split(["\r\n","\n"],StringSplitOptions.None);
            List<string> sourceLines=lines
                .Where(line=>!string.IsNullOrWhiteSpace(line))
                .Where(line=>!inputNames.Any(name=>Regex.IsMatch(line,$@"^\s*{Regex.Escape(name)}\s*=")))
                .ToList();
            string sourceCode=string.Join(Environment.NewLine,sourceLines);
            List<(int Start,int End,string Name)> identifiers=GetPythonIdentifiers(sourceCode);
            HashSet<string> assignedNames=identifiers
                .Where(token=>IsVariableAssignment(sourceCode,token.Start,token.End))
                .Select(token=>token.Name)
                .ToHashSet(StringComparer.Ordinal);
            HashSet<string> functionNames=identifiers
                .Where(token=>IsFunctionName(sourceCode,identifiers,token.Start,token.End))
                .Select(token=>token.Name)
                .ToHashSet(StringComparer.Ordinal);
            List<string> variables=inputNames
                .Concat(identifiers
                    .Where(token=>!PythonKeywords.Contains(token.Name))
                    .Where(token=>!functionNames.Contains(token.Name) || assignedNames.Contains(token.Name))
                    .Where(token=>!IsAttributeName(sourceCode,token.Start) || assignedNames.Contains(token.Name))
                    .Where(token=>!PythonBuiltins.Contains(token.Name) || assignedNames.Contains(token.Name))
                    .Select(token=>token.Name))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            string maskedCode=MaskVariables(sourceCode,identifiers,variables.ToHashSet(StringComparer.Ordinal));

            return new BlockVariableResponse{
                CodeBlocks=maskedCode.Split(["\r\n","\n"],StringSplitOptions.RemoveEmptyEntries).ToList(),
                Variables=variables
            };
        }

        private List<(int Start,int End,string Name)> GetPythonIdentifiers(string code){
            List<(int Start,int End,string Name)> identifiers=new();
            for(int i=0;i<code.Length;){
                char current=code[i];
                if(current=='#'){
                    int lineEnd=code.IndexOfAny(['\r','\n'],i);
                    i=lineEnd<0 ? code.Length : lineEnd;
                    continue;
                }
                if(current is '\'' or '"'){
                    i=SkipPythonString(code,i);
                    continue;
                }
                if(char.IsLetter(current) || current=='_'){
                    int start=i++;
                    while(i<code.Length && (char.IsLetterOrDigit(code[i]) || code[i]=='_')){
                        i++;
                    }

                    string name=code[start..i];
                    if(i<code.Length && code[i] is '\'' or '"' &&
                        name is "r" or "R" or "u" or "U" or "b" or "B" or "f" or "F" or
                            "br" or "Br" or "bR" or "BR" or "rb" or "Rb" or "rB" or "RB" or
                            "fr" or "Fr" or "fR" or "FR" or "rf" or "Rf" or "rF" or "RF"){
                        i=SkipPythonString(code,i);
                        continue;
                    }
                    identifiers.Add((start,i,name));
                    continue;
                }
                i++;
            }
            return identifiers;
        }

        private int SkipPythonString(string code,int start){
            char quote=code[start];
            int quoteLength=start+2<code.Length &&
                code[start+1]==quote && code[start+2]==quote ? 3 : 1;
            bool escaped=false;
            int i=start+quoteLength;
            while(i<code.Length){
                if(escaped){
                    escaped=false;
                    i++;
                    continue;
                }
                if(code[i]=='\\'){
                    escaped=true;
                    i++;
                    continue;
                }
                if(code[i]==quote && (quoteLength==1 ||
                    i+2<code.Length && code[i+1]==quote && code[i+2]==quote)){
                    return i+quoteLength;
                }
                i++;
            }
            return code.Length;
        }

        private bool IsVariableAssignment(string code,int start,int end){
            int next=end;
            while(next<code.Length && char.IsWhiteSpace(code[next])){
                next++;
            }
            if(next<code.Length && (code[next]=='=' && (next+1>=code.Length || code[next+1]!='=') ||
                code[next]==':' && next+1<code.Length && code[next+1]=='=')){
                return true;
            }
            if(next+1<code.Length && code[next+1]=='=' &&
                code[next] is '+' or '-' or '*' or '/' or '%' or '@' or '&' or '|' or '^'){
                return true;
            }

            int previous=start-1;
            while(previous>=0 && char.IsWhiteSpace(code[previous])){
                previous--;
            }
            int wordEnd=previous+1;
            while(previous>=0 && (char.IsLetterOrDigit(code[previous]) || code[previous]=='_')){
                previous--;
            }
            string precedingWord=code[(previous+1)..wordEnd];
            return precedingWord is "for" or "as";
        }

        private bool IsFunctionName(string code,List<(int Start,int End,string Name)> identifiers,int start,int end){
            int next=end;
            while(next<code.Length && char.IsWhiteSpace(code[next])){
                next++;
            }
            if(next<code.Length && code[next]=='('){
                return true;
            }

            int tokenIndex=identifiers.FindIndex(token=>token.Start==start);
            return tokenIndex>0 && identifiers[tokenIndex-1].Name=="def";
        }

        private bool IsAttributeName(string code,int start){
            int previous=start-1;
            while(previous>=0 && char.IsWhiteSpace(code[previous])){
                previous--;
            }
            return previous>=0 && code[previous]=='.';
        }

        private string MaskVariables(string code,List<(int Start,int End,string Name)> identifiers,HashSet<string> variables){
            StringBuilder output=new(code.Length);
            int position=0;
            foreach((int start,int end,string name) in identifiers){
                if(!variables.Contains(name)){
                    continue;
                }
                output.Append(code,position,start-position);
                output.Append(Constants.OAIConstants.blank);
                position=end;
            }
            output.Append(code,position,code.Length-position);
            return output.ToString();
        }

        public async Task<StructuredResult> GetStructuredResult(int difficultyScore){
//            if(difficultyScore is < 0 or > 100){
  //              throw new ArgumentOutOfRangeException(nameof(difficultyScore),"Difficulty score must be between 0 and 100.");
    //        }

            Console.WriteLine($"[Agent workflow] Starting generation (difficulty {difficultyScore}/100).");
            String gotVars = Variables.GetVar();
            LogicalExtermination.Components.Pages.Game.setVars(gotVars);
            List<string> list_inputs= new List<string> {gotVars};//GetInputs(1);
            List<string> inputNames=GetInputVariableNames(list_inputs);
            string inputText=StringListToString(list_inputs);
            Console.WriteLine($"[Agent workflow] Selected input variables: {string.Join(", ",inputNames)}.");

            Console.WriteLine("[Agent workflow] Step 1/4: Generating programming problem.");
            string problem;
            try{
                problem=await GenerateProblemResponse($"""
                    Difficulty score: {difficultyScore} out of 100.
                    Use these exact supplied variable definitions:
                    {inputText}
                    Generate one concise programming task that fits the score and can be solved using only these values.
                """);
                Console.WriteLine($"[Agent workflow] Problem generated: {problem}");
            }catch(Exception exception){
                Console.WriteLine($"[Agent workflow] Problem generation failed: {exception}");
                throw;
            }

            string answerPrompt=$"""
                Programming task:
                {problem}

                Requested difficulty: {difficultyScore} out of 100.
                Supplied variable definitions (preserve their names and values exactly):
                {inputText}

                Write the complete executable Python solution.
            """;

            Console.WriteLine("[Agent workflow] Step 2/4: Generating executable Python solution.");
            string generatedCode;
            try{
                generatedCode=await GenerateAnswerResponse(answerPrompt);
                Console.WriteLine($"[Agent workflow] Solution generated ({generatedCode.Length} characters).");
            }catch(Exception exception){
                Console.WriteLine($"[Agent workflow] Solution generation failed: {exception}");
                throw;
            }

            string completedCode=BuildCompletedCode(list_inputs,generatedCode,inputNames);
            string terminalOutput="";
            for(int attempt=0;attempt<5;attempt++){
                Console.WriteLine($"[Agent workflow] Step 3/4: Running Python solution (attempt {attempt+1}/5).");
                terminalOutput=await GetTerminalOutput(completedCode);
                Console.WriteLine($"[Agent workflow] Python execution output: {terminalOutput}");
                if(!string.IsNullOrWhiteSpace(terminalOutput) &&
                    terminalOutput!="Error Occurred with Python Code"){
                    Console.WriteLine("[Agent workflow] Python solution executed successfully.");
                    break;
                }

                if(attempt==4){
                    Console.WriteLine("[Agent workflow] Python solution failed all five attempts.");
                    throw new InvalidOperationException("The generated Python solution did not run successfully after five attempts.");
                }

                Console.WriteLine("[Agent workflow] Asking the answer agent to fix the failed solution.");
                answerPrompt+=$"""

                    The previous solution failed to run or produced no output. Fix it.
                    Previous solution:
                    {completedCode}
                    Runtime result:
                    {terminalOutput}
                """;
                try{
                    generatedCode=await GenerateAnswerResponse(answerPrompt);
                    Console.WriteLine($"[Agent workflow] Revised solution generated ({generatedCode.Length} characters).");
                }catch(Exception exception){
                    Console.WriteLine($"[Agent workflow] Solution revision failed: {exception}");
                    throw;
                }
                completedCode=BuildCompletedCode(list_inputs,generatedCode,inputNames);
            }

            Console.WriteLine("[Agent workflow] Step 4/4: Formatting player code blocks.");
            BlockVariableResponse blockResponse=GenerateBlockVariableResponse(completedCode,inputNames);
            Console.WriteLine($"[Agent workflow] Created {blockResponse.CodeBlocks.Count} code blocks and {blockResponse.Variables.Count} variable choices.");
            StructuredResult result=new(){
                Problem=problem,
                Codeblocks=blockResponse.CodeBlocks,
                CompletedCode=completedCode,
                Variables=blockResponse.Variables,
                TerminalOutput=terminalOutput
            };

            Console.WriteLine("[Agent workflow] Shuffling blocks and returning structured result.");
            return processResponse(result);
        }

        public async Task<StructuredResult> TestGetStructuredResult(){
            var result = new StructuredResult{ 
                Problem = "Given the input list ['apple', 'banana', 'cherry'], create a Python code snippet that prints each fruit on a new line.",
                Codeblocks = new List<string>
                {
                    "___ = ['apple', 'banana', 'cherry']",
                    "for fruit in ___:",
                    "print(fruit)",
                },
                CompletedCode = "for fruit in input_list:\nprint(fruit)",
                Variables = new List<string> { "input_list" },
                TerminalOutput = "apple banana cherry"
            };

            BlockVariableResponse blocks=GenerateBlockVariableResponse(result.CompletedCode,result.Variables);
            result.Codeblocks=blocks.CodeBlocks;
            result.Variables=blocks.Variables;
            return processResponse(result);
        }

        private async Task<string> GetTerminalOutput(string code){
            return await pythonRunner.RunTestPy(code);
        }

        private async Task<string> GenerateProblemResponse(string prompt){
            var response=await problem_agent.RunAsync<ProblemResponse>(prompt);
            if(string.IsNullOrWhiteSpace(response.Result.Problem)){
                throw new InvalidOperationException("The problem agent returned an empty problem statement.");
            }
            return response.Result.Problem;
        }

        private async Task<string> GenerateAnswerResponse(string prompt){
            var response=await answer_agent.RunAsync<AnswerResponse>(prompt);
            if(string.IsNullOrWhiteSpace(response.Result.Answer)){
                throw new InvalidOperationException("The answer agent returned an empty Python solution.");
            }

            return Regex.Replace(response.Result.Answer.Trim(),@"^\s*```(?:python)?\s*|\s*```\s*$","",RegexOptions.IgnoreCase).Trim();
        }
    }
}