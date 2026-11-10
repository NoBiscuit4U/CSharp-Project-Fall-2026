namespace Backend;

public class CodeBrick{
    public string code { get; set; }
    public int varCount { get; set; }
    public List<string> variables { get; set; }

    public CodeBrick(string text, int count)
    {
        code = text;
        varCount = count;
        variables = new List<string>();
        for (int i = 0; i < varCount; i++)
        {
            variables.Add("");
        }
    }
}