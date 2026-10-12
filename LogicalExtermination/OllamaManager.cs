using OllamaSharp;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Microsoft.Extensions.AI;

public record ProblemResponse(){
    public required string Problem {get;set;}=null;
    public required List<string> Codeblocks {get;set;}=null;
}

public delegate ProblemResponse ProcessResponse(ProblemResponse response);

namespace Backend{
    sealed class OllamaManager{
        private string Model;
        private OllamaApiClient ollama;

        private readonly ProcessResponse ProcessResponse;

        public OllamaManager(string Model){
            this.Model=Model;

            ProcessResponse=response =>{
                response.Problem ??= "Error Occurred";
                response.Codeblocks ??= new List<string> {"EMPTY"};
                return response;
            };

            ProcessResponse+=response =>{
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

        public async Task<ProblemResponse> GenerateProblemQuestion(List<dynamic> list_inputs){
            string prompt=string.Format(Constants.OllamaConstants.prompt_editable_question,list_inputs);
            ProblemResponse result=new ProblemResponse{Problem=null,Codeblocks=null};;

            try{
                var response=await ollama.GetResponseAsync<ProblemResponse>(prompt);
                result=response.Result;
            }catch(Exception e){
                Console.WriteLine(e);
            }

            return ProcessResponse(result);
        }

        public async Task<ProblemResponse> GenerateProblemAnswer(List<dynamic> list_inputs){
            string prompt=string.Format(Constants.OllamaConstants.prompt_editable_answer,list_inputs);
            ProblemResponse result=new ProblemResponse{Problem=null,Codeblocks=null};

            try{
                var response=await ollama.GetResponseAsync<ProblemResponse>(prompt);
                result=response.Result;
            }catch(Exception e){
                Console.WriteLine(e);
            }

            return ProcessResponse(result);
        }
    }
}