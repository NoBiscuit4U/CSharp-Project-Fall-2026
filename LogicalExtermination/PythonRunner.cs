using System;
using System.Text;
using Backend;
using IronPython.Hosting;
using IronPython.Runtime.Operations;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Scripting.Hosting;

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

}