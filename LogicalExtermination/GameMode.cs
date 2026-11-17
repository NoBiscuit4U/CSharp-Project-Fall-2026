using System;


namespace Backend
{
    public class GameMode
    {
        public int difficulty = 0;
        public int health = 5;
        private PythonRunner pythonRunner=new PythonRunner();
        private OAIManager oai=null!;
        private StructuredResult resultQ = null;
        public string problem;
        public string completed_code;
        public List<string> variables;
        public string terminal_out;
        public List<string> codeblocks;
        public static String vars = "";

        public void damage()
        {
            if (health > 0)
            {
                health -= 1;                
            }
        }
        public async Task GetTest(){
            Console.WriteLine("GetTest() Started");
            var oai = new OAIManager(pythonRunner);
            Console.WriteLine("Running...");
            resultQ=null;
            try{
                resultQ=await oai.GetStructuredResult(difficulty);
            }catch(InvalidOperationException e){
                while(resultQ==null){
                    resultQ=await oai.GetStructuredResult(difficulty);
                }
            }

            problem=resultQ.Problem;
            completed_code=resultQ.CompletedCode;
            codeblocks=resultQ.Codeblocks;
            variables=resultQ.Variables;
            terminal_out=resultQ.TerminalOutput;
            Console.WriteLine("GetTest() Finished");
        }
        public async Task<Boolean> CheckAnswer(List<CodeBrick> active){
            var test_term_out=await pythonRunner.RunTestPy(active);

            if(test_term_out==terminal_out){
                return true;
            }else{
                return false;
            }
        }
        public static void setVars(String v)
        {
            vars = v;
        }
    }
}