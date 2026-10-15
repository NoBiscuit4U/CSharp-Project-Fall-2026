using System.Runtime.ConstrainedExecution;

namespace Backend{
    class Constants{
        public class OllamaConstants{
            public static string gemma2b="gemma:2b";
            public static string qwencoder2_5_7b="qwen2.5-coder:7b";
            public static string qwen2_5_3b="qwen2.5-coder:3b";

            public static string prompt_editable_question="""
                ##PERSONA##
                You are a Python programming problem generator. 
                You will be given a list of variables with values that will be used as inputs for the program.
                Use the variables with assigned values and write a problem question related to them.

                ##INPUT AND VAR LIST##
                {0}
                
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
                Only output the problem statement in the Output key value in the Schema.
                """;

            public static string prompt_editable_answer="""
                ##PERSONA##
                You are a program solver for a Python program. You will be provided a list of inputs and a problem statement.
                You are not to solve the problem, but only to write a program in Python to solve it.

                ##INPUT LIST##
                {0}

                ##PROBLEM STATEMENT##
                {1}

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
                    - DO NOT USE ``` ANYWHERE
            """;

            public static string prompt_editable_blocks="""
                ##PERSONA##
                You are a code block generator. You will take in a completed python program and turn it into code blocks.
                Each block of code is exactly one line of python code.
                Do not edit any code, use the code provided below, and turn it into blocks.
                
                ##COMPLETED CODE##
                {0}

                ##INSTRUCTION##
                Do not edit any code, only turn each line into a code block.
                Take all unique variables from the program and output them in a list.
                    -Do not output the values of the variables only the variable names.s
                    -Do not remove the variables from the code only collect the unique variable names.

                ##OUTPUT EXAMPLE##
                Only change the Codeblocks and Variables key values in then output.
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