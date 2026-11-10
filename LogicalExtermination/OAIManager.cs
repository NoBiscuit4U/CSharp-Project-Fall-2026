using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using IronPython.Runtime.Operations;
using Microsoft.Agents.AI;
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
        private readonly ProcessResponse processResponse;
        private readonly PythonRunner pythonRunner;

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

        private List<string> GetInputs(int num){
            return Constants.OAIConstants.static_var_val_string
                .OrderBy(_ => Random.Shared.Next())
                .Take(num)
                .ToList();
        }

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
            string maskedCode=MaskInputVariables(string.Join(Environment.NewLine,sourceLines),inputNames);

            return new BlockVariableResponse{
                CodeBlocks=maskedCode.Split(["\r\n","\n"],StringSplitOptions.RemoveEmptyEntries).ToList(),
                Variables=inputNames
            };
        }

        private string MaskInputVariables(string code,List<string> inputNames){
            HashSet<string> variables=inputNames.ToHashSet(StringComparer.Ordinal);
            List<char> output=new();
            char quote='\0';
            int quoteLength=0;
            bool escaped=false;

            for(int i=0;i<code.Length;){
                char current=code[i];

                if(quote!='\0'){
                    output.Add(current);
                    if(escaped){
                        escaped=false;
                        i++;
                        continue;
                    }
                    if(current=='\\'){
                        escaped=true;
                        i++;
                        continue;
                    }
                    if(current==quote && (quoteLength==1 ||
                        i+2<code.Length && code[i+1]==quote && code[i+2]==quote)){
                        if(quoteLength==3){
                            output.Add(code[i+1]);
                            output.Add(code[i+2]);
                            i+=3;
                        }else{
                            i++;
                        }
                        quote='\0';
                        quoteLength=0;
                        continue;
                    }
                    i++;
                    continue;
                }

                if(current=='#'){
                    int end=code.IndexOfAny(['\r','\n'],i);
                    if(end<0){
                        output.AddRange(code[i..]);
                        break;
                    }
                    output.AddRange(code[i..end]);
                    i=end;
                    continue;
                }
                if(current is '\'' or '"'){
                    quote=current;
                    quoteLength=i+2<code.Length &&
                        code[i+1]==current && code[i+2]==current ? 3 : 1;
                    for(int j=0;j<quoteLength;j++){
                        output.Add(code[i+j]);
                    }
                    i+=quoteLength;
                    continue;
                }
                if(char.IsLetter(current) || current=='_'){
                    int end=i+1;
                    while(end<code.Length && (char.IsLetterOrDigit(code[end]) || code[end]=='_')){
                        end++;
                    }

                    string identifier=code[i..end];
                    output.AddRange(variables.Contains(identifier)
                        ? Constants.OAIConstants.blank
                        : identifier);
                    i=end;
                    continue;
                }

                output.Add(current);
                i++;
            }

            return new string(output.ToArray());
        }

        public async Task<StructuredResult> GetStructuredResult(int difficultyScore=50){
            if(difficultyScore is < 0 or > 100){
                throw new ArgumentOutOfRangeException(nameof(difficultyScore),"Difficulty score must be between 0 and 100.");
            }

            Console.WriteLine($"[Agent workflow] Starting generation (difficulty {difficultyScore}/100).");
            List<string> list_inputs=GetInputs(1);
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
            for(int attempt=0;attempt<3;attempt++){
                Console.WriteLine($"[Agent workflow] Step 3/4: Running Python solution (attempt {attempt+1}/3).");
                terminalOutput=await GetTerminalOutput(completedCode);
                Console.WriteLine($"[Agent workflow] Python execution output: {terminalOutput}");
                if(!string.IsNullOrWhiteSpace(terminalOutput) &&
                    terminalOutput!="Error Occurred with Python Code"){
                    Console.WriteLine("[Agent workflow] Python solution executed successfully.");
                    break;
                }
                if(attempt==2){
                    Console.WriteLine("[Agent workflow] Python solution failed all three attempts.");
                    throw new InvalidOperationException("The generated Python solution did not run successfully after three attempts.");
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