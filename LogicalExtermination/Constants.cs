using System.Runtime.ConstrainedExecution;

namespace Backend{
    class Constants{
        public class OllamaConstants{
            public static string gemma2b="gemma:2b";
            public static string qwencoder2_5_7b="qwen2.5-coder:7b";

            public static string prompt_editable_question="""
                ##PERSONA##
                You are a Pyhon programming problem generator. 
                You will be given a list of variable values that will be used as inputs for the program.

                ##INPUT LIST##
                {0}
                
                ##TASK##
                Using the provided input list above, ask a programming question for a programming {1}.

                ##OUTPUT##
                Only output the problem statement.
                Do not provide any other information.
                """;

            public static string prompt_editable_answer="""
                ##PERSONA##
                You are a program solver for a Python program. You will be provided a list of inputs and a problem statement.
                Using these you will generate programming blocks to solve this problem.

                ##INPUT LIST##
                {0}

                ##PROBLEM STATEMENT##
                {1}

                ##INSTRUCTION##
                1. Each element in 'Codeblocks' must be exactly ONE valid, executable Python line.
                2. Do NOT use string concatenation with '+' (e.g. "a" + "b"). Use string interpolation ($"...") or simple variables instead.
                3. Use single quotes (') for literal strings inside code lines (e.g. 'hello') to avoid string escaping conflicts.
                4. Do not leave placeholder comments like '// add code here'. Write complete code.
                5. Write simple code that is easy to understand. Avoid complex one-liners or advanced Python features.

                ##OUTPUT EXAMPLE##
                - Problem: "Write a program that finds the longest word in a list."
                - Input: "input = ['apple', 'banana', 'cherry']"
                - Codeblocks:
                [
                    "longest = ''"
                    "for word in input:"
                    "if word.length() > longest.length():"
                    "longest = word"
                    "print(word)"
                ]
                """;

            public static List<string> static_var_val_string= new List<string>{
                "Apple",
                "Orange",
            };
        }

        public class PythonConstants{
            public static string py_test_file="test.py";
        }
    }
}