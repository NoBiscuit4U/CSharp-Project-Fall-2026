using System.Runtime.ConstrainedExecution;

namespace Backend{
    class Constants{
        public class OllamaConstants{
            public static string gemma2b="gemma:2b";
            public static string qwencoder2_5_7b="qwen2.5-coder:7b";
            public static string qwen2_5_3b="qwen2.5-coder:3b";
            public static string problem_agent_instructions="""
                ##PERSONA##
                You are a Python programming problem generator. 
                You will be given a list of variables with values that will be used as inputs for the program.
                Use the variables with assigned values and write a problem question related to them.
                
                ##TASK##
                Using the provided variable and input list above, ask a programming question for a programming novice.
                Do not just refer to the values associated with the variable but the variable with the values
                   -  Example: Dont say use the input list ['apple', 'banana', 'cherry'] as the input, say use the variable and values input_list=['apple', 'banana', 'cherry']
                Ensure that it does not require any external packages or libraries or using import.
                Do not create other variables and inputs only use the variables and inputs in the list provided above.
                    - You may generate additional variables if needed for the program but they should be assigned a value given through the original input
                        - Example: A variable longest, in order to store the longest variable in a list, etc
                Do not use input() in order to get an input, only use the hardcoded variables and values present in the INPUT AND VAR LIST

                ##OUTPUT##
                Only output the problem statement in the Problem key value in the Schema.
            """;
            public static string answer_agent_instructions="""
                ##PERSONA##
                You are a program solver for a Python program. You will be provided a list of inputs and a problem statement.
                You are not to solve the problem, but only to write a program in Python to solve it.
                You will be given a list of variables with values, and a problem statement.

                ##INSTRUCTION##
                Write a python program to solve the problem, ensure that it can be run in one file.
                    - Output a final value using print().
                Ensure you do not pull from outside libraries or packages using import.
                Ensure that variables are defined and that it will run and provide a terminal output.
                Ensure it displays a human readable output.

                ##OUTPUT##
                Make sure the Output is a complete Python script, not an output that says the answer.
                Output the completed pythob program into the Output key values in the Schema.
                Do not output using back ticks or encase it
                    - DO NOT: ```python .... ```
                    - DO NOT USE ``` ANYWHER;
            """;
            public static string blocking_agent_instructions="""
                ##PERSONA##
                You are a code block generator. You will take in a completed python program and turn it into code blocks.
                Each block of code is exactly one line of python code.
                Do not edit any code, use the code provided below, and turn it into blocks.

                ##INSTRUCTION##
                Do not edit any code, only turn each line into a code block.
                Take all unique variables from the program and output them in a list.
                    -Do not output the values of the variables only the variable names.s
                    -Do not remove the variables from the code only collect the unique variable names.

                ##OUTPUT EXAMPLE##
                Only change the Codeblocks and Variables key values in then output.
            """;
            public static string overseer_agent_instructions="""
                ##PERSONA##
                You are an Overseer agent, you will call other agents and tools, in order to return a structured output.
                You will be provided a list of tools and given a basic structure to follow in order to fulfill the structured output.

                ##TOOLS##
                You will be provided tools with the name of the tools, and then required information for the tool for agent call
                    - GenerateProblemResponse(string prompt)
                        - Requires
                            - Prompt needs the variable and value list
                        - Returns
                            - Problem Statement
                    - GenerateAnswerResponse(string prompt)
                        - Requires
                            - Prompt needs the variable and value list
                            - Prompt needs the Problem Statement
                        - Returns
                            - Completed Python Code
                    - GenerateBlockVariableResponse(string prompt)
                        - Requires
                            - Completed and Verified Python Code
                        - Returns
                            - List of a list of Variable and CodeBlocks
                    - GetTerminalOutput(string code)
                        - Runs a python script and verifies it works
                        - Returns
                            - Terminal Output
                    - StringListToString(List<string> list)
                        - Returns the string version of the list of strings

                ##INSTRUCTION##
                Given the tools above and their specifications, you will run the loop described below, and call ShowCurrentStep every turn with your current step and process
                Use StringListToString to turn Lists of strings a readable string for both yourself and the agentsn when needed.
                    1. Call GenerateProblemResponse
                    2. Call GenerateAnswerResponse
                    3. Verify the code generated by GenerateAnswerResponse
                        3a. Call GetTerminalOutput with the code generated
                        4a. If GetTerminalOutput returns nothing or Error Occurred with Python Code re call GenerateAnswerResponse providing the
                            failed code and asking for the agent to fix the code.
                        5a. If the GetTerminalOutput returns a valid print statement output leave this loop
                        6a. Do this loop a max of 3 times, if it fails more than that, restart from the problem statement.
                    4. Call GenerateBlockVariableResponse
                    5. Review the Data fixing any obvious errors, and compiling the data in the required fields in the provided schema.
            """;

            public static List<string> static_var_val_string= new List<string>{
                "input = ['apple', 'banana', 'cherry']",
                "input = ['apple', 'banana', 'cherry']",
                "input = ['apple', 'banana', 'cherry']"
            };

            public static string blank="___";
        }

        public class PythonConstants{
            public static string py_test_file="test.py";
        }
    }
}