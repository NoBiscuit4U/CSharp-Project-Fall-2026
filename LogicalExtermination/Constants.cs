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
                You will be given a list of variable values that will be used as inputs for the program.

                ##INPUT LIST##
                {0}
                
                ##TASK##
                Using the provided input list above, ask a programming question for a programming novice.
                Ensure that it does not require any external packages or libraries or using import.

                ##OUTPUT##
                Only output the problem statement in the Problem key value in the Schema.
                """;

            public static string prompt_editable_answer="""
                ##PERSONA##
                You are a program solver for a Python program. You will be provided a list of inputs and a problem statement.
                Using the inputs below to you will write a program in ONLY Python to solve the problem and provide a terminal output using print()

                ##INPUT LIST##
                {0}

                ##PROBLEM STATEMENT##
                {1}

                ##INSTRUCTION##
                Ensure you do not pull from outside libraries or packages using import.
                Ensure that variables are defined and that it will run and provide a terminal output.
                Ensure it displays a human readable output.

                ##OUTPUT##
                Output the completed program into the CompletedCode key values in the Schema.
                DO NOT USE BACKTICKS, ie ```
            """;

            public static string prompt_editable_blocks="""
                ##PERSONA##
                You are a code block generator. You will take in a completed python program and turn it into code blocks.
                
                ##COMPLETED CODE##
                {0}

                ##INSTRUCTION##
                1. Each element in 'Codeblocks' must be exactly ONE valid, executable Python line.
                2. Do NOT use string concatenation with '+' (e.g. "a" + "b"). Use string interpolation ($"...") or simple variables instead.
                3. Use single quotes (') for literal strings inside code lines (e.g. 'hello') to avoid string escaping conflicts.
                4. Do not leave placeholder comments like '// add code here'. Write complete code.
                5. Write simple code that is easy to understand. Avoid complex one-liners or advanced Python features.

                6. Remove all unique variables from the program and output them in a list.
                    - In the place of the variable use BLANK to denotate that a variable was in this spot.
                    - ie replace the variable longest with BLANK or the variable maximum with BLANK

                ##OUTPUT EXAMPLE##
                Only change the Codeblocks and Variables key values in then output but it should look like this.
                - Codeblocks:[
                    "BLANK = ''"
                    "for word in input:"
                    "   if word.length() > BLANK.length():"
                    "       BLANK = word"
                    "print(BLANK)"
                ]
                - Variables:[
                    longest
                ]
                "
                """;

            public static List<string> static_var_val_string= new List<string>{
                "input = ['apple', 'banana', 'cherry']",
                "input = ['apple', 'banana', 'cherry']",
                "input = ['apple', 'banana', 'cherry']"
            };
        }

        public class PythonConstants{
            public static string py_test_file="test.py";
        }
    }
}