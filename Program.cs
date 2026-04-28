using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MiniLParser
{
    // ===========================================================================
    // Phase 2 - MiniL Language Syntax Parser
    // Student: Mohamed Hesham Abdelhamid 
    // ID:324243444
    // ===========================================================================

    class Token
    {
        public string Value { get; set; }
        public string Type { get; set; }
        public int Line { get; set; }
        public Token(string value, string type, int line) { Value = value; Type = type; Line = line; }
    }

    class Scanner
    {
        private static readonly HashSet<string> Keywords = new HashSet<string> { "num", "text", "check", "then", "otherwise", "repeat", "until" };
        private static readonly List<(string Pattern, string Type)> TokenRules = new List<(string, string)>
        {
            (@"//[^\n]*", "SKIP"),
            (@":=", "Assign_Op"), 
            (@"\+", "Plus_Op"), (@"-", "Minus_Op"), (@"\*", "Multiply_Op"), (@"/", "Divide_Op"),
            (@">", "Greater_Than_Op"), (@"<", "Less_Than_Op"), (@"=", "Equal_Op"), (@"!=", "NotEqual_Op"),
            (@";", "Semicolon"), (@"\(", "LeftParen"), (@"\)", "RightParen"), 
            (@"\{", "LeftBrace"), (@"\}", "RightBrace"),
            (@"\d+\.\d+", "Number"), (@"\d+", "Number"),
            (@"[a-zA-Z][a-zA-Z0-9]*", "Identifier"), (@"[ \t\r\n]+", "SKIP")
        };

        public List<Token> Tokenise(string source)
        {
            var tokens = new List<Token>();
            int pos = 0; int line = 1;
            while (pos < source.Length)
            {
                bool matched = false;
                foreach (var (pattern, type) in TokenRules)
                {
                    var regex = new Regex(@"\G" + pattern);
                    var m = regex.Match(source, pos);
                    if (!m.Success) continue;
                    if (type != "SKIP") {
                        string finalType = (type == "Identifier" && Keywords.Contains(m.Value)) ? "Keyword" : type;
                        tokens.Add(new Token(m.Value, finalType, line));
                    }
                    line += m.Value.Count(c => c == '\n');
                    pos += m.Length; matched = true; break;
                }
                if (!matched) throw new Exception($"Scanner Error: Unknown character '{source[pos]}' at line {line}");
            }
            return tokens;
        }
    }

    class MiniLParser
    {
        private readonly List<Token> _tokens;
        private int _index;
        private Token Current => _index < _tokens.Count ? _tokens[_index] : new Token("EOF", "EOF", -1);

        public MiniLParser(List<Token> tokens) { _tokens = tokens; _index = 0; }

        private void Match(string expected) {
            if (Current.Value == expected || Current.Type == expected) { _index++; return; }
            throw new Exception($"Syntax Error: Expected '{expected}' but found '{Current.Value}' at line {Current.Line}");
        }

        private bool Check(string expected) => Current.Value == expected || Current.Type == expected;

        public void ParseProgram() { ParseStatements(); }

        private void ParseStatements() {
            ParseStatement();
            if (Check(";")) {
                Match(";");
                if (_index < _tokens.Count && !Check("}") && !Check("until") && !Check("otherwise"))
                    ParseStatements();
            }
        }

        private void ParseStatement() {
            if (Check("num") || Check("text")) {
                _index++; 
                Match("Identifier"); Match(":="); Exp();
            }
            else if (Check("check")) {
                Match("check"); Match("("); Cond(); Match(")"); Match("then");
                ParseStatements();
                if (Check("otherwise")) { Match("otherwise"); ParseStatements(); }
            }
            else if (Check("repeat")) {
                Match("repeat"); ParseStatements(); Match("until"); Match("("); Cond(); Match(")");
            }
            else if (Check("{")) { Match("{"); ParseStatements(); Match("}"); }
            else if (Current.Type == "Identifier") { Match("Identifier"); Match(":="); Exp(); }
            else throw new Exception($"Syntax Error: Unexpected token '{Current.Value}' at line {Current.Line}");
        }

        private void Cond() {
            Exp();
            if (Check("Greater_Than_Op") || Check("Less_Than_Op") || Check("Equal_Op") || Check("NotEqual_Op")) _index++;
            else throw new Exception($"Syntax Error: Missing operator in condition at line {Current.Line}");
            Exp();
        }

        private void Exp() { Term(); while (Check("Plus_Op") || Check("Minus_Op")) { _index++; Term(); } }
        private void Term() { Fact(); while (Check("Multiply_Op") || Check("Divide_Op")) { _index++; Fact(); } }
        private void Fact() {
            if (Check("LeftParen")) { Match("("); Exp(); Match(")"); }
            else if (Current.Type == "Identifier" || Current.Type == "Number") _index++;
            else throw new Exception($"Syntax Error: Invalid expression at line {Current.Line}");
        }
    }

    class Program
    {
        static void Main()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=====================================================");
            Console.WriteLine("  Phase 2 - MiniL Syntax Parser");
            Console.WriteLine("  Student: Mohamed Hesham Abdelhamid / ID: 324243444");
            Console.WriteLine("=====================================================");
            Console.ResetColor();

            const string file = "input.txt";
            if (!File.Exists(file)) {
                Console.WriteLine("Error: input.txt not found.");
                return;
            }

            try {
                var tokens = new Scanner().Tokenise(File.ReadAllText(file));
                new MiniLParser(tokens).ParseProgram();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n[✔] SUCCESS: Your code is syntactically valid.");
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[✘] {ex.Message}");
            }
            Console.ResetColor();
            Console.WriteLine("\nEnd of output");
            Console.ReadKey();
        }
    }
}
