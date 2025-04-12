using System.Text.RegularExpressions;

namespace ThirdTask.Services;

public partial class BooleanSearch
{

    private readonly Dictionary<string, List<string>> _index;
    private readonly IEnumerable<string> _fileNames;
    
    public BooleanSearch(
        Dictionary<string, List<string>> index)
    {
        _index = index;
        _fileNames = index.Values
            .SelectMany(x => x)
            .Distinct();
    }
        
    public IEnumerable<string> Search(string input)
    {
        var tokens = Tokenize(input);
        var postfix = ToPostfix(tokens);

        var docs = SearchDocs(postfix)
            .Order(new NaturalSortComparer());
        return docs;
    }

    private static string[] Tokenize(string input)
    {
        var replaced = input
            .Replace("ИЛИ", " | ")
            .Replace("И", " & ")
            .Replace("!", " ! ")
            .Replace("НЕ", " ! ")
            .ToLower();

        return replaced.Split(" ", StringSplitOptions.RemoveEmptyEntries);
    }

    private static List<string> ToPostfix(IEnumerable<string> tokens)
    {
        var output = new List<string>();
        var stack = new Stack<string>();
        var priorities = new Dictionary<string, int>
        {
            ["!"] = 3,
            ["&"] = 2,
            ["|"] = 1
        };

        foreach (var token in tokens)
        {
            if (token is "&" or "|" or "!")
            {
                while (stack.Count > 0 &&
                       priorities.TryGetValue(stack.Peek(), out var prec) &&
                       prec >= priorities[token])
                    output.Add(stack.Pop());
                stack.Push(token);
            }
            else 
                output.Add(token);
        }

        while (stack.Count > 0)
            output.Add(stack.Pop());

        return output;
    }

    private IEnumerable<string> SearchDocs(IEnumerable<string> tokens)
    {
        var stack = new Stack<IEnumerable<string>>();
        
        foreach (var token in tokens)
        {
            switch (token)
            {
                case "&" or "|":
                {
                    var right = stack.Pop();
                    var left = stack.Pop();
                    var combined = token == "&" ?
                        And(left, right) :
                        Or(left, right);
                    stack.Push(combined);
                    break;
                }
                case "!":
                {
                    var operand = stack.Pop();
                    var notExpr = Not(operand);
                    stack.Push(notExpr);
                    break;
                }
                default:
                {
                    var constant = Constant(token);
                    stack.Push(constant);
                    break;
                }
            }
        }

        return stack.Pop();
    }

    private IEnumerable<string> And(IEnumerable<string> a, IEnumerable<string> b) => a.Intersect(b);

    private IEnumerable<string> Or(IEnumerable<string> a, IEnumerable<string> b) => a.Union(b);

    private IEnumerable<string> Not(IEnumerable<string> a) => _fileNames.Except(a);

    private IEnumerable<string> Constant(string token) => 
        !_index.TryGetValue(token, out var value) ?
            [] :
            value;
    
    [GeneratedRegex("[0-9]+")]
    private static partial Regex NumbersRegex();
}