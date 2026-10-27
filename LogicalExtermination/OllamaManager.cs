using OllamaSharp;
using System;
using System.Runtime.InteropServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using IronPython.Runtime.Operations;

public record StructuredResult(){
    public required string Problem {get;set;}=null;
    public required List<string> Codeblocks {get;set;}=null;
    public required string CompletedCode {get;set;}=null;
    public required List<string> Variables {get;set;}=null;
    public required string TerminalOutput {get;set;}=null;
}
public record BlockVariableResponse(){
    public required List<string> CodeBlocks {get;set;}=null;
    public required List<string> Variables {get;set;}=null;
}
public record ProblemResponse(){
    public required string Problem {get;set;}=null;
}

public record AnswerResponse(){
    public required string Answer {get;set;}=null;
}

public delegate StructuredResult ProcessResponse(StructuredResult response);

namespace Backend{
    sealed class OllamaManager{
        private readonly string Model;
        private readonly OllamaApiClient ollama_endpoint;
        private readonly ProcessResponse processResponse;
        private readonly PythonRunner pythonRunner;

        private readonly AIAgent problem_agent;
        private readonly AIAgent answer_agent;
        private readonly AIAgent blocking_agent;
        private readonly AIAgent overseer_agent;

        public OllamaManager(string Model,PythonRunner pythonRunner){
            this.Model=Model;
            this.pythonRunner=pythonRunner;

            var uri=new Uri("http://localhost:11434");

            ollama_endpoint=new OllamaApiClient(uri,Model);

            answer_agent=ollama_endpoint.AsAIAgent(
                    instructions:Constants.OllamaConstants.answer_agent_instructions,
                    name:"Answer Agent");
            blocking_agent=ollama_endpoint.AsAIAgent(
                    instructions:Constants.OllamaConstants.blocking_agent_instructions,
                    name:"Blocking Agent");
            ollama_endpoint.SelectedModel=Constants.OllamaConstants.mistralnemo;
            problem_agent=ollama_endpoint.AsAIAgent(
                    instructions:Constants.OllamaConstants.problem_agent_instructions,
                    name:"Problem Agent");
            overseer_agent=ollama_endpoint.AsAIAgent(
                    instructions:Constants.OllamaConstants.overseer_agent_instructions,
                    name:"Overseer",
                    tools:[AIFunctionFactory.Create(GenerateProblemResponse),AIFunctionFactory.Create(GenerateAnswerResponse),
                        AIFunctionFactory.Create(GenerateBlockVariableResponse),AIFunctionFactory.Create(GetTerminalOutput),
                        AIFunctionFactory.Create(StringListToString)]
                );

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
                return response;
            };
        }

        private string StringListToString(List<string> list){
            return string.Join(Environment.NewLine,list);
        }

        private List<string> GetInputs(int num){
            return Constants.OllamaConstants.static_var_val_string
                .OrderBy(_ => Random.Shared.Next())
                .Take(num)
                .ToList();
        }

        private void ReplaceVars(StructuredResult result){
            result.Variables=result.Variables.OrderByDescending(s=>s.Length).ToList();

            List<string> finalized_blocks=new List<string>();
            foreach(string block in result.Codeblocks){
                string final_block=block;
                foreach(string var in result.Variables){
                    final_block=final_block.replace(var,Constants.OllamaConstants.blank);
                }

                finalized_blocks.Add(final_block);
            }
            Console.WriteLine("FINALIZED_BLOCKS:{0}",string.Join(", \n",finalized_blocks));
            result.Codeblocks=finalized_blocks;
        }

        public async Task<StructuredResult> GetStructuredResult(){
            List<string> list_inputs=GetInputs(1);

            var response=await overseer_agent.RunAsync<StructuredResult>($"""
                Use the following variables with values and generate the provided structured result:
                {StringListToString(list_inputs)}
            """);
            StructuredResult result=response.Result;
            ReplaceVars(result);


            return result;
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

            ReplaceVars(result);
            return result;
        }

        private async Task<string> GetTerminalOutput(string code){
            return pythonRunner.RunTestPy(code).Result;
        }

        private async Task<string> GenerateProblemResponse(string prompt){
            ProblemResponse result=new ProblemResponse{Problem=null};

            try{
                var response=await problem_agent.RunAsync<ProblemResponse>(prompt);
                result=response.Result;
            }catch(Exception e){
                Console.WriteLine(e);
            }

            Console.WriteLine(result.Problem);
            return result.Problem;
        }

        private async Task<string> GenerateAnswerResponse(string prompt){
            AnswerResponse result=new AnswerResponse{Answer=null};

            try{
                var response=await answer_agent.RunAsync<AnswerResponse>(prompt);
                result=response.Result;
            }catch(Exception e){
                Console.WriteLine(e);
            }

            Console.WriteLine(result.Answer);
            return result.Answer;
        }

        private async Task<List<List<string>>> GenerateBlockVariableResponse(string prompt){
            BlockVariableResponse result=new BlockVariableResponse{CodeBlocks=null,Variables=null};

            try{
                var response=await blocking_agent.RunAsync<BlockVariableResponse>(prompt);
                result=response.Result;
            }catch(Exception e){
                Console.WriteLine(e);
            }

            Console.WriteLine($"CodeBlocks:{result.CodeBlocks}");
            Console.WriteLine($"Variables:{result.Variables}");
            return new List<List<string>>{result.CodeBlocks,result.Variables};
        }
    }
}