namespace Backend{
    class Constants{
        public class OAIConstants{
            public static string default_model="gpt-4o-mini";
            public static string problem_agent_instructions="""
                ##PERSONA##
                You are a Python programming problem generator. 
                You will be given a list of variables with values that will be used as inputs for the program.
                Use the variables with assigned values and write a problem question related to them.
                
                ##TASK##
                Use the provided difficulty score and variable definitions to create a programming question for a programming novice.
                Interpret difficulty scores on a 0-100 scale:
                    - 0-20: basic output, simple variable use, or a single string operation.
                    - 21-40: simple string manipulation or a short sequence of list operations.
                    - 41-60: a loop or conditional applied to the supplied value.
                    - 61-80: combine a loop or conditional with multiple related operations.
                    - 81-100: a multi-step algorithm with clear, beginner-appropriate reasoning.
                Higher scores must require more reasoning, not obscure Python syntax.
                Select a task that is possible with the supplied values and built-in Python features.
                State the supplied variable name and its value in the problem so the player knows which data is available.
                Ensure that it does not require any external packages or libraries or using import.
                Do not require additional input, external packages, imports, or behavior not supported by the supplied values.
                State one specific, testable task and the expected form of the printed result.
                Do not include a solution or code in the problem statement.
        
                ##OUTPUT##
                Only output the problem statement in the Problem key value in the Schema.
            """;
            public static string answer_agent_instructions="""
                ##PERSONA##
                You are a program solver for a Python program. You will be provided a list of inputs and a problem statement.
                You are not to solve the problem, but only to write a program in Python to solve it.
                You will be given a list of variables with values, and a problem statement.

                ##INSTRUCTION##
                Write one complete, executable Python script that solves the task.
                Preserve every supplied variable name and value exactly; do not redefine or replace them.
                Use only Python built-ins. Do not import modules, call input(), or depend on external data.
                Keep the solution appropriate for the requested difficulty and print a clear, human-readable result.
                Ensure the script runs from top to bottom and prints at least one line.

                ##OUTPUT##
                Make sure the Output is a complete Python script, not an output that says the answer.
                Return only the complete Python script in the Answer field.
                Do not include markdown fences, explanations, or the problem statement.
            """;
            public static List<string> static_var_val_string= new List<string>{
                "word = 'butterfly'",
                "fruits = ['apple', 'banana', 'cherry']",
                "numbers = [12, 4, 19, 7, 3]",
                "sentence = 'Learning Python is fun'",
                "scores = [72, 85, 91, 64, 78]"
            };

            public static string blank="___";
        }

        public class PythonConstants{
            public static string py_test_file="test.py";
        }
    }
}