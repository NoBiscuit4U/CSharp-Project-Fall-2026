using System;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace Backend{
    class OllamaManager{
        private string model;
        private OllamaApiClient ollama;

        public OllamaManager(string model){
            this.model = model;

            Initalize();
        }

        private void Initalize(){
            var uri = new Uri("http://localhost:11434");

            ollama = new OllamaApiClient(uri);
            ollama.SelectedModel=this.model;
        }

        public async Task test(){
            var chat = new Chat(ollama);
            string returnval="";

            await foreach (var answerToken in chat.SendAsync("Generate a simple programming error problem."))
                returnval+=answerToken;

            Console.WriteLine(returnval);
        }
    }
}