using System.Runtime.ConstrainedExecution;

namespace Backend{
    class Constants{
        public class OllamaConstants{
            public static string gemma2b="gemma:2b";

           public static string prompt_editable = """
            You are a C# programming problem generator.
            
            TASK:
            Generate a programming challenge with a difficulty rating of {0} out of 100.
            You MUST generate exactly {1} single-line C# code blocks that solve the problem.
            
            """;

        public static string prompt_static = """
            RULES FOR CODE BLOCKS:
            1. Each element in 'Codeblocks' must be exactly ONE valid, executable C# line.
            2. Do NOT use string concatenation with '+' (e.g. "a" + "b"). Use string interpolation ($"...") or simple variables instead.
            3. Use single quotes (') for literal strings inside code lines (e.g. 'hello') to avoid string escaping conflicts.
            4. Do not leave placeholder comments like '// add code here'. Write complete code.

            EXAMPLE CODE STRUCTURE:
            - Problem: "Write a program that finds the longest word in a list."
            - Codeblocks:
              [
                "var words = new List<string> { 'apple', 'banana', 'cherry' };",
                "var longest = words.OrderByDescending(w => w.Length).First();",
                "Console.WriteLine($\"Longest word: {longest}\");"
              ]
            """;
        }
    }
}