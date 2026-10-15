using OllamaSharp;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Microsoft.Extensions.AI;
using System.Reflection.Metadata;
using Microsoft.Scripting.Ast;
using IronPython.Runtime.Operations;

public record ProblemResponse(){
    public required string Problem {get;set;}=null;
    public required List<string> Codeblocks {get;set;}=null;
    public required string CompletedCode {get;set;}=null;
    public required List<string> Variables {get;set;}=null;
    public required string TerminalOutput {get;set;}=null;
}
public record StringListResponse(){
    public required List<string> StringList {get;set;}=null;
}
public record StringResponse(){
    public required string Output {get;set;}=null;
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

        private List<string> ReplaceVars(List<string> codeblocks,List<string> variables){
            variables=variables.OrderByDescending(s=>s.Length).ToList();

            List<string> finalized_blocks=new List<string>();
            foreach(string block in codeblocks){
                string final_block=block;
                foreach(string var in variables){
                    final_block=final_block.replace(var,Constants.OllamaConstants.blank);
                }

                finalized_blocks.Add(final_block);
            }
            Console.WriteLine("FINALIZED_BLOCKS:{0}",string.Join(", \n",finalized_blocks));
            return finalized_blocks;
        }

        private async Task<string> GenerateProblem(string prompt,string list_inputs){
            string response = await GenerateStringResponse(string.Format(prompt,list_inputs));
            return response;
        }

        private async Task<string> GenerateAnswer(string prompt,string list_inputs,string problem_statement){
            string response = await GenerateStringResponse(string.Format(prompt,list_inputs,problem_statement));
            return response;
        }

        private async Task<ProblemResponse> GenerateBlocks(string prompt,string completed_code){
            ProblemResponse response = await GenerateProblemResponse(string.Format(prompt,completed_code));
            return response;
        }

        private async Task<List<string>> FormatGeneratedBlocks(string prompt,string code_blocks){
            List<string> response = await GenerateStringListResponse(string.Format(prompt,code_blocks));
            return response;
        }

        public async Task<ProblemResponse> GetProblemResponse(){
            List<string> list_inputs=GetInputs(1);
            bool verified=false;
            string terminal_output="";
            int max_trys=10;
            int i=0;

            string problem=await GenerateProblem(Constants.OllamaConstants.prompt_editable_question,string.Join(", \n",list_inputs));
            //Console.WriteLine("Problem:{0}",problem);

            string completed_code=await GenerateAnswer(Constants.OllamaConstants.prompt_editable_answer,string.Join(", \n",list_inputs),problem);
            //Console.WriteLine("Completed_Code: {0}",completed_code);

            //TODO: Refine and Make Iterative Instead of Hardcoded Max Trys
            while(!verified && max_trys!=i){
                try{
                    terminal_output=await pythonRunner.RunTestPy(completed_code);
                    //Console.WriteLine("Terminal_Output:{0}",terminal_output);

                    verified=true;
                    break;
                }catch(Exception e){
                    completed_code=await GenerateAnswer(Constants.OllamaConstants.prompt_editable_answer,string.Join(", \n",list_inputs),problem);

                    if(max_trys <= i){
                        return new ProblemResponse{Problem=null,Codeblocks=null,CompletedCode=null,Variables=null,TerminalOutput=null};
                    }

                    i+=1;
                }  
            }

            ProblemResponse response=await GenerateBlocks(Constants.OllamaConstants.prompt_editable_blocks,completed_code);
            Console.WriteLine("Blocks:{0}",string.Join(", \n",response.Codeblocks));
            Console.WriteLine("Variables:{0}",string.Join(", \n",response.Variables));

            response.Problem=problem;
            response.CompletedCode=completed_code;
            response.TerminalOutput=terminal_output;
            response.Codeblocks=ReplaceVars(response.Codeblocks,response.Variables);

            //Console.WriteLine("Returning Response");
            return processResponse(response);
        }

         private async Task<ProblemResponse> GenerateProblemResponse(string prompt){
            ProblemResponse result=new ProblemResponse{Problem=null,Codeblocks=null,CompletedCode=null,Variables=null,TerminalOutput=null};

            try{
                var response=await ollama.GetResponseAsync<ProblemResponse>(prompt);
                result=response.Result;
            }catch(Exception e){
                Console.WriteLine(e);
            }

            return result;
        }

        private async Task<string> GenerateStringResponse(string prompt){
            StringResponse result=new StringResponse{Output=null};

            try{
                var response=await ollama.GetResponseAsync<StringResponse>(prompt);
                result=response.Result;
            }catch(Exception e){
                Console.WriteLine(e);
            }

            return result.Output;
        }

        private async Task<List<string>> GenerateStringListResponse(string prompt){
            StringListResponse result=new StringListResponse{StringList=null};

            try{
                var response=await ollama.GetResponseAsync<StringListResponse>(prompt);
                result=response.Result;
            }catch(Exception e){
                Console.WriteLine(e);
            }

            return result.StringList;
        }
    }
}