using OllamaSharp;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Microsoft.Extensions.AI;

public record ProblemResponse(){
    public required string Problem {get;set;}=null;
    public required List<string> Codeblocks {get;set;}=null;
    public required string CompletedCode {get;set;}=null;
    public required List<string> Variables {get;set;}=null;
    public required string TerminalOutput {get;set;}=null;
}

public delegate ProblemResponse ProcessResponse(ProblemResponse response);

namespace Backend{
    sealed class OllamaManager{
        private string Model;
        private OllamaApiClient ollama;
        private readonly ProcessResponse processResponse;
        private readonly PythonRunner pythonRunner;

        public OllamaManager(string Model,PythonRunner pythonRunner){
            this.Model=Model;
            this.pythonRunner=pythonRunner;

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

            Initalize();
        }

        private void Initalize(){
            var uri=new Uri("http://localhost:11434");

            ollama=new OllamaApiClient(uri);
            ollama.SelectedModel=Model;
        }

        private double GetBlockCount(double difficultyScore){
            return Math.Ceiling(difficultyScore/5);
        }

        private List<string> GetInputs(int num){
            return Constants.OllamaConstants.static_var_val_string
                .OrderBy(_ => Random.Shared.Next())
                .Take(num)
                .ToList();
        }

        private async Task<string> GenerateProblem(string prompt,List<string> list_inputs){
            ProblemResponse response = await Generate(string.Format(prompt,list_inputs));
            return response.Problem;
        }

        private async Task<string> GenerateAnswer(string prompt,List<string> list_inputs,string problem_statement){
            ProblemResponse response = await Generate(string.Format(prompt,list_inputs,problem_statement));
            return response.CompletedCode;
        }

        private async Task<ProblemResponse> GenerateBlocks(string prompt,string completed_code){
            ProblemResponse response = await Generate(string.Format(prompt,completed_code));
            return response;
        }

        public async Task<ProblemResponse> GetProblemResponse(){
            List<string> list_inputs=GetInputs(1);
            bool verified=false;
            string terminal_output="";
            int max_trys=10;
            int i=0;

            string problem=await GenerateProblem(Constants.OllamaConstants.prompt_editable_question,list_inputs);
            Console.WriteLine("Problem:{0}",problem);

            string completed_code=await GenerateAnswer(Constants.OllamaConstants.prompt_editable_answer,list_inputs,problem);
            Console.WriteLine("Completed_Code: {0}",completed_code);

            while(!verified && max_trys!=i){
                try{
                    terminal_output=await pythonRunner.RunTestPy(completed_code);
                    Console.WriteLine("Terminal_Output:{0}",terminal_output);

                    verified=true;
                    break;
                }catch(Exception e){
                    completed_code=await GenerateAnswer(Constants.OllamaConstants.prompt_editable_blocks,list_inputs,problem);

                    if(max_trys <= i){
                        return new ProblemResponse{Problem=null,Codeblocks=null,CompletedCode=null,Variables=null,TerminalOutput=null};
                    }

                    i+=1;
                }  
            }

            ProblemResponse response=await GenerateBlocks(Constants.OllamaConstants.prompt_editable_blocks,completed_code);

            response.Problem=problem;
            response.CompletedCode=completed_code;
            response.TerminalOutput=terminal_output;

            return response;

        }

        private async Task<ProblemResponse> Generate(string prompt){
            ProblemResponse result=new ProblemResponse{Problem=null,Codeblocks=null,CompletedCode=null,Variables=null,TerminalOutput=null};

            try{
                var response=await ollama.GetResponseAsync<ProblemResponse>(prompt);
                result=response.Result;
            }catch(Exception e){
                Console.WriteLine(e);
            }

            return processResponse(result);
        }
    }
}