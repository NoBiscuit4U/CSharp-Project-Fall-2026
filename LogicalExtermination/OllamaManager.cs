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

        private List<string> GetInputs(){
            return new List<string>{};
        }

        private async Task<string> GenerateProblem(string prompt,List<string> list_inputs){
            ProblemResponse response = await Generate(string.Format(prompt,list_inputs));
            return response.Problem;
        }

        private async Task<string> GenerateAnswer(string prompt,List<string> list_inputs,string problem_statement){
            ProblemResponse response = await Generate(string.Format(prompt,list_inputs,problem_statement));
            return response.CompletedCode;
        }

        private async Task<string> GenerateBlocks(string prompt,string completed_code){
            ProblemResponse response = await Generate(string.Format(prompt,completed_code));
            return response.Problem;
        }

        public async Task<ProblemResponse> GetProblemResponse(){
            List<string> list_inputs=GetInputs();
            string problem=await GenerateProblem(Constants.OllamaConstants.prompt_editable_answer,list_inputs);

            return new ProblemResponse{Problem=null,Codeblocks=null,CompletedCode=null,Variables=null};

        }

        private async Task<ProblemResponse> Generate(string prompt){
            ProblemResponse result=new ProblemResponse{Problem=null,Codeblocks=null,CompletedCode=null,Variables=null};

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