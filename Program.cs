using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Windows.Forms;

namespace MiniLParser
{
    // ===========================================================================
    // Phase 2 - MiniL Language Syntax Parser
    // Student: Mohamed Hesham Abdelhamid / ID: 324243444
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
            (@"/\*[\s\S]*?\*/", "SKIP"),
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

        public void ParseProgram() 
        { 
            ParseStatements(); 
            if (Current.Type != "EOF")
            {
                throw new Exception($"Syntax Error: Unexpected symbols after program end '{Current.Value}' at line {Current.Line}");
            }
        }  

        private void ParseStatements() {  
            if (Current.Type == "EOF" || Check("}") || Check("until") || Check("otherwise")) return;

            ParseStatement();  
            if (Check(";")) {
                Match(";");  
                if (_index < _tokens.Count && !Check("}") && !Check("until") && !Check("otherwise"))  
                    ParseStatements();  
            }
        }  

        private void ParseStatement() {  
            if (Check("check")) {  
                Match("check");   
                Cond(); 
                Match("then");  
                ParseStatements();  
                if (Check("otherwise")) { Match("otherwise"); ParseStatements(); }  
            }  
            else if (Check("repeat")) {  
                Match("repeat");   
                ParseStatements();   
                Match("until");   
                Cond(); 
            }  
            else if (Check("{")) { Match("{"); ParseStatements(); Match("}"); }  
            else if (Current.Type == "Identifier") {  
                Match("Identifier");  
                if (!Check(":=")) throw new Exception($"Syntax Error: Assignment must use ':=' not '{Current.Value}' at line {Current.Line}");  
                Match(":="); Exp();  
            }  
            else throw new Exception($"Syntax Error: A statement cannot start with '{Current.Value}' ({Current.Type})");  
        }  

        private void Cond() {  
            Exp();  
            if (Check("Greater_Than_Op") || Check("Less_Than_Op") || Check("Equal_Op") || Check("NotEqual_Op"))   
                Match(Current.Value);   
            else throw new Exception($"Syntax Error: Missing operator in condition at line {Current.Line}");  
            Exp();  
        }  

        private void Exp() { 
            Term(); 
            while (Check("Plus_Op") || Check("Minus_Op")) { 
                Match(Current.Value); 
                Term(); 
            } 
        }  

        private void Term() { 
            Fact(); 
            while (Check("Multiply_Op") || Check("Divide_Op")) { 
                Match(Current.Value); 
                Fact(); 
            } 
        }  

        private void Fact() {  
            if (Check("LeftParen")) { Match("("); Exp(); Match(")"); }  
            else if (Current.Type == "Identifier") Match("Identifier");   
            else if (Current.Type == "Number") Match("Number");   
            else throw new Exception($"Syntax Error: Invalid expression at line {Current.Line}");  
        }  
    }  

    class Program  
    {  
        [STAThread]
        static void Main()  
        {  
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MiniLGui());
        }  
    }

    class MiniLGui : Form 
    {
        RichTextBox inputArea;
        DataGridView tokenGrid;
        Label statusLbl;
        Label infoLbl;

        public MiniLGui() 
        {
            this.Text = "MiniL Parser - Mohamed Hesham (324243444)";
            this.Size = new Size(850, 650);
            this.StartPosition = FormStartPosition.CenterScreen;

            infoLbl = new Label();
            infoLbl.Text = "Student: Mohamed Hesham Abdelhamid | ID: 324243444";
            infoLbl.Location = new Point(20, 10);
            infoLbl.AutoSize = true;
            infoLbl.Font = new Font("Arial", 11, FontStyle.Bold);
            infoLbl.ForeColor = Color.MidnightBlue;

            inputArea = new RichTextBox();
            inputArea.Location = new Point(20, 70);
            inputArea.Size = new Size(450, 350);
            inputArea.Font = new Font("Consolas", 11);

            Button btnRun = new Button();
            btnRun.Text = "Run Analysis";
            btnRun.Location = new Point(20, 430);
            btnRun.Size = new Size(130, 45);
            btnRun.BackColor = Color.LightSkyBlue;

            tokenGrid = new DataGridView();
            tokenGrid.Location = new Point(490, 70);
            tokenGrid.Size = new Size(320, 350);
            tokenGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tokenGrid.AllowUserToAddRows = false;
            tokenGrid.Columns.Add("L", "Lexeme");
            tokenGrid.Columns.Add("T", "Type");
            tokenGrid.Columns.Add("Li", "Line");

            statusLbl = new Label();
            statusLbl.Location = new Point(20, 490);
            statusLbl.Size = new Size(790, 90);
            statusLbl.BorderStyle = BorderStyle.FixedSingle;
            statusLbl.BackColor = Color.White;
            statusLbl.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            btnRun.Click += new EventHandler(btnRun_Click);

            this.Controls.Add(infoLbl);
            this.Controls.Add(inputArea);
            this.Controls.Add(btnRun);
            this.Controls.Add(tokenGrid);
            this.Controls.Add(statusLbl);
        }

        void btnRun_Click(object sender, EventArgs e)
        {
            try 
            {
                tokenGrid.Rows.Clear();
                string code = inputArea.Text;
                
                Scanner sc = new Scanner();
                var tokens = sc.Tokenise(code);

                foreach (var t in tokens)
                {
                    tokenGrid.Rows.Add(t.Value, t.Type, t.Line);
                }

                MiniLParser parser = new MiniLParser(tokens);
                parser.ParseProgram();

                statusLbl.Text = "[✔] SUCCESS: Your code is syntactically valid.";
                statusLbl.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                statusLbl.Text = "[✘] " + ex.Message;
                statusLbl.ForeColor = Color.Red;
            }
        }
    }
}
