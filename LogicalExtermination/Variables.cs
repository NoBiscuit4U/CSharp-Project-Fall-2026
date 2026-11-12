using System;
using System.Runtime.InteropServices;
using LogicalExtermination.Components.Pages;

namespace Backend
{
    public class Variables
    {
        private static Random rand = new Random();
        public static string GetVar()
        {
            int dif = Game.getDifficulty();
            int scale = (int)MathF.Ceiling(dif / 10.0f);
            String output = "";
            int type = rand.Next(0, 5 + scale); //Range from 0,6 - 0,16 
            if (type < 2) { output = GetWord(); }
            else if (type >= 2 && type < 6) { output = GetNums(); }
            else if (type >= 6 && type < 7) { output = GetNumList(); }
            else if (type >= 7 && type < 9) { output = GetWords(); }
            else if (type >= 9 && type < 12) { output = GetBigNumList(); }
            else if (type >= 12 && type < 13) { output = GetWordList(); }
            else if (type >= 13 && type < 15) { output = GetPhrase(); }
            else if (type >= 15) { output = GetPhrase(); }
            Console.WriteLine(output);
            return output;
        }
        // word, nums, numList, words, bigNumList, wordList, phrase, dictionary
        private static List<string> words()
        {
            List<string> output = new List<string>();
            int type = rand.Next(0, 3);
            if (type == 0)
            {
                output = new() { "Apple", "Banana", "Orange", "Grape", "Peach", "Pear", "Mango", "Lemon", "Cherry", "Plum" };
            }
            if (type == 1)
            {
                output = new() { "Tiger", "Rabbit", "Monkey", "Elephant", "Giraffe", "Zebra", "Turtle", "Kitten", "Panda", "Dolphin" };
            }
            if (type == 2)
            {
                output = new() { "Red", "Blue", "Green", "Yellow", "Purple", "Orange", "Pink", "Brown", "Silver", "Golden" };
            }
            return output;
        }
        private static string GetWord()
        {
            string output = $"word = '{words()[rand.Next(0, 10)]}'";
            return output;
        }
        private static string GetNums()
        {
            string output = "";
            int count = rand.Next(1, 7);
            int name = rand.Next(0, 3);
            List<string> vars = new List<string>();
            if (count == 1)
            {
                if (name == 0) { vars = new List<string> {"a"}; }
                else if (name == 1) { vars = new List<string> {"x"}; }
                else if (name == 2) { vars = new List<string> {"n"}; }
                output = $"{vars[0]} = {rand.Next(1, 10)}";
            }
            if (count == 2 || count == 3)
            {
                if (name == 0) { vars = new List<string> {"a", "b"}; }
                else if (name == 1) { vars = new List<string> {"x", "y"}; }
                else if (name == 2) { vars = new List<string> {"i", "j"}; }
                output = $"{vars[0]} = {rand.Next(1, 10)}, {vars[1]} = {rand.Next(1, 10)}";
            }
            if (count == 4 || count == 5)
            {
                if (name == 0) { vars = new List<string> {"a", "b", "c"}; }
                else if (name == 1) { vars = new List<string> {"x", "y", "z"}; }
                else if (name == 2) { vars = new List<string> {"f", "g", "h"}; }
                output = $"{vars[0]} = {rand.Next(1, 10)}, {vars[1]} = {rand.Next(1, 10)}, {vars[2]} = {rand.Next(1, 10)}";
            }
            if (count == 7)
            {
                if (name == 0) { vars = new List<string> {"a", "b", "c", "d"}; }
                else if (name == 1) { vars = new List<string> {"w", "x", "y", "z"}; }
                else if (name == 2) { vars = new List<string> {"a", "b", "c", "d"}; }
                output = $"{vars[0]} = {rand.Next(1, 10)}, {vars[1]} = {rand.Next(1, 10)}, {vars[2]} = {rand.Next(1, 10)}, {vars[3]} = {rand.Next(1, 10)}";
            }
            return output;
        }
        private static string GetNumList()
        {
            String output = "input = [";
            int count = rand.Next(2, 6);
            for (int i = 0; i < count; i++)
            {
                int num = rand.Next(1, 10);
                output += num.ToString();
                if (i < count - 1) { output += ", "; }
            }
            output += "]";
            return output;
        }
        private static string GetWords()
        {
            string output = "";
            int count = rand.Next(2, 4);
            List<string> wordList = words();
            if (count == 2)
            {
                output = $"word1 = ['{wordList[rand.Next(0, 10)]}', word2 ='{wordList[rand.Next(0, 10)]}']";
            }
            if (count == 3)
            {
                output = $"word1 = ['{wordList[rand.Next(0, 10)]}', word2 ='{wordList[rand.Next(0, 10)]}', word3 ='{wordList[rand.Next(0, 10)]}']";
            }
            return output;
        }
        private static string GetBigNumList()
        {
            String output = "input = [";
            int count = rand.Next(2, 6);
            int type = rand.Next(0, 2);
            if (type == 0)
            {
                for (int i = 0; i < count; i++)
                {
                    int num = rand.Next(1, 100);
                    output += num.ToString();
                    if (i < count - 1) { output += ", "; }
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    float num = rand.Next(1, 100)/10.0f;
                    output += num.ToString();
                    if (i < count - 1) { output += ", "; }
                }
            }
            output += "]";
            return output;
        }
        private static string GetWordList()
        {
            string output = "input = [";
            int count = rand.Next(2, 5);
            List<string> wordList = words();
            for (int i = 0; i < count; i++)
            {
                output += $"'{wordList[rand.Next(0, 10)]}'";
                if (i < count - 1) { output += ", "; }
            }
            output += "]";
            return output;
        }
        private static string GetPhrase()
        {
            int type = rand.Next(0, 4);
            string output = "";
            if (type == 0) { output = "sentence = 'Learning Python is fun!'"; }
            if (type == 1) { output = "sentence = 'Today is going to be a great day.'"; }
            if (type == 2) { output = "sentence = 'No, I am your father.'"; }
            if (type == 3) { output = "sentence = 'Don't worry, AI won't take over.'"; }
            return output;
        }
        private static string GetDictionary()
        {
            string output = "";
            return output;
        }
    }
}