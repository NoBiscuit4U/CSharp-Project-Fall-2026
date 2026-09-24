using System;
using Microsoft.Extensions.AI;
using OllamaSharp;
using System.Text.Json;
using System.Collections;

delegate Dictionary<string,object> Translate(string json);

namespace Backend{
    class OllamaManager{
        private string model;
        private OllamaApiClient ollama;

        private double difficultyScore=100;

        private Translate translator;

        public OllamaManager(string model){
            this.model = model;
            translator=new Translate(TranslateJSON);
            
            Initalize();
        }

        private void Initalize(){
            var uri = new Uri("http://localhost:11434");

            ollama = new OllamaApiClient(uri);
            ollama.SelectedModel=this.model;
        }

        private static string Clean(string json){
            string cleanedJson=json.Trim();

            if (cleanedJson.StartsWith("```json"))
            {
                cleanedJson = cleanedJson.Substring(7);
            }
            else if (cleanedJson.StartsWith("```"))
            {
                cleanedJson = cleanedJson.Substring(3); 
            }

            if (cleanedJson.EndsWith("```"))
            {
                cleanedJson = cleanedJson.Substring(0, cleanedJson.Length - 3);
            }

            cleanedJson = cleanedJson.Trim();

            return cleanedJson;
        }

        private Dictionary<string,object> TranslateJSON(string json){
            return JsonSerializer.Deserialize<Dictionary<string, object>>(Clean(json));
        }

        public async Task<Dictionary<string,object>> GenerateProblemDynamic(){
            var chat = new Chat(ollama);
            string output="";

            await foreach (var answerToken in chat.SendAsync(
            $"""
                You are a Programming Problem Generator. Your goal is to create problems based off of a difficulty score, and then respective
                code blocks to complete the problem.

                **DIFFICULTY SCORE**
                - {difficultyScore}

                **INSTRUCTIONS**
                - Programming Problem
                    - Use the Difficulty Score above to determine the difficulty and number of blocks
                    - The number of blocks should be based off this formula **FORMULA**:(Math.Ceil({difficultyScore}/4))
                        - EXAMPLE: If difficulty score is 100, then their should be 25 individual blocks
                        - Each block should be one line of code used in the program
                    - The difficulty of the program should consist of harder concepts and a more vague prompt
                        - Difficulty Score is between 1-100, 1 being the lowest and 100 being the highest
            """+
            """
                **STRUCTURE**
                - JSON
                - "problem" is the problem statement it "Make me a program that finds the longest word in the list"
                - Inside of the "problem_code_blocks" list, create a seperate string for each **LINE OF CODE** used in the program
                    - NUMBER OF BLOCK: 1-n, where n is the number of code blocks determined the **FORMULA** in Instructions
                - EXAMPLE:
                {
                    "problem":""
                    "problem_code_blocks":[]
                }

                **OUTPUT**
                - Only Output the JSON
                    - Do not provide any other information or discuss the output.
                - CRITICAL: Return ONLY raw JSON. Do not wrap the JSON in markdown code blocks, do not use ```json, and do not include any surrounding text.

            """))
                output+=answerToken;

            return translator(output);
        }
    }
}