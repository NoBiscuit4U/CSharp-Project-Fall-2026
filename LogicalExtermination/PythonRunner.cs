using System;
using System.Text;
using Backend;
using IronPython.Hosting;
using System.Text.RegularExpressions;
using Microsoft.Scripting.Hosting;

namespace Backend{
    public delegate string ProcessCode(List<string> blocks);
    class PythonRunner{
        private  readonly ScriptEngine Engine;
        private  readonly ScriptScope Scope;

        private readonly ProcessCode ProcessCode;
        public PythonRunner(){
            Engine=Python.CreateEngine();
            Scope=Engine.CreateScope();

            ProcessCode=blocks=>{
                return string.Join(Environment.NewLine,blocks);
            };
        }

        private async Task WriteCode(List<string> blocks){
            File.WriteAllText(Constants.PythonConstants.py_test_file,ProcessCode(blocks));
        }

        private async Task WriteCode(string code){
            File.WriteAllText(Constants.PythonConstants.py_test_file,code);
        }

        private async Task WriteCode(List<CodeBrick> code){
            List<string> blocks=new List<string>{};
            foreach(CodeBrick brick in code){
                foreach(string var in brick.variables){
                    var search=Regex.Escape(Constants.OAIConstants.blank);
                    
                    Regex regex=new Regex(search);
                    brick.code=regex.Replace(brick.code,var,1);
                }

                blocks.Add(brick.code);
            }

            Console.WriteLine(string.Join(Environment.NewLine,blocks));
            File.WriteAllText(Constants.PythonConstants.py_test_file,ProcessCode(blocks));
        }

        public async Task<string> RunTestPy(List<string> blocks){
            await WriteCode(blocks);
            ScriptSource script=Engine.CreateScriptSourceFromFile(Constants.PythonConstants.py_test_file);

            using MemoryStream memoryStream=new MemoryStream();

            Engine.Runtime.IO.SetOutput(memoryStream,Encoding.UTF8);

            
            script.Execute(Scope);

            string terminal_out=Encoding.UTF8.GetString(memoryStream.ToArray());
            return terminal_out; 
        }

        public async Task<string> RunTestPy(string code){
            await WriteCode(code);
            ScriptSource script=Engine.CreateScriptSourceFromFile(Constants.PythonConstants.py_test_file);

            using MemoryStream memoryStream=new MemoryStream();
            Engine.Runtime.IO.SetOutput(memoryStream,Encoding.UTF8);

            try{
                script.Execute(Scope);

                string terminal_out=Encoding.UTF8.GetString(memoryStream.ToArray());
                return terminal_out;
            }catch(Exception e){
                    Console.WriteLine("Error Occurred: {0}", e);
                    return "Error Occurred with Python Code";
            }
        }

        public async Task<string> RunTestPy(List<CodeBrick> codeBricks){
            await WriteCode(codeBricks);
            ScriptSource script=Engine.CreateScriptSourceFromFile(Constants.PythonConstants.py_test_file);

            using MemoryStream memoryStream=new MemoryStream();
            Engine.Runtime.IO.SetOutput(memoryStream,Encoding.UTF8);

            try{
                script.Execute(Scope);

                string terminal_out=Encoding.UTF8.GetString(memoryStream.ToArray());
                return terminal_out;
            }catch(Exception e){
                Console.WriteLine("Error Occurred: {0}", e);
                return "Error Occurred with Python Code";
            }
        }

    }
}