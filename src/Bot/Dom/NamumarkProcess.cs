namespace Sugarmaple.TheSeed.Namumark;

using Sugarmaple.TheSeed.Namumark.Parsing;
using System;
using System.Diagnostics.CodeAnalysis;

internal class NamumarkProcess
{
    private readonly string _raw;
    internal static string? _recentOne;

    public NamumarkProcess(string raw)
    {
        _recentOne = _raw = raw;
    }

    public ASTNode ParseAsToken()
    {
        var paragraphList = new List<ASTNode>();
        var ret = new ASTNode(ASTNodeType.Document, 0, _raw.Length, new() {
            new ASTNode(ASTNodeType.Document, 0, _raw.Length, paragraphList)
        });

        if (TryGetRedirct(out var redirect))
        {
            var paraRedToken = new ASTNode(ASTNodeType.Paragraph, 0, 0, new() {
                default,
                new ASTNode(ASTNodeType.Content, redirect.Index, redirect.Length, new() { redirect }),
            });

            paragraphList.Add(paraRedToken);
            return ret;
        }

        var index = 0;
        var heading = new ASTNode(ASTNodeType.Heading, 0, 0);
        while (index < _raw.Length)
        {
            var content = GetClauses(ref index, out var newHeading);
            paragraphList.Add(new(ASTNodeType.Paragraph, heading.Index, content.Index + content.Length - heading.Index, new() { heading, content }));
            heading = newHeading;
        }
        if (heading.Type == ASTNodeType.Heading)
        {
            paragraphList.Add(new(ASTNodeType.Paragraph, heading.Index, _raw.Length - heading.Index,
                new() { heading, new ASTNode(ASTNodeType.Content, _raw.Length - 1, 0, new()) }));
        }
        return ret;
    }

    private bool TryGetRedirct([MaybeNullWhen(false)] out ASTNode token)
    {
        const string Head = "#redirect ";
        token = default;
        if (!_raw.StartsWith(Head)) return false;

        var anchor = _raw.AsSpan(Head.Length).IndexOf('#');
        var refEnd = _raw.Length - Head.Length - 1;
        if (anchor == -1)
            anchor = refEnd;

        token = new(ASTNodeType.Redirect, 0, _raw.Length)
        {
            new(ASTNodeType.None, Head.Length, anchor),
        };
        if (anchor < refEnd)
        {
            token.Children.Add(new(ASTNodeType.None, anchor + 1, refEnd - (anchor - 1)));
        }
        return true;
    }

    private ASTNode GetClauses(ref int index, out ASTNode heading)
    {
        var creator = new ExpressionCreator();
        var tape = new StringTape(_raw, index, _raw.Length);
        var ret = creator.GetFreeElementList(tape);
        if (ret.Children.Count > 0)
        {
            heading = ret.Children[^1];
            if (heading.Type == ASTNodeType.Heading)
            {
                ret.Children.RemoveAt(ret.Children.Count - 1);
                ret.Length -= heading.Length;
            }
        }
        else heading = default;
        index = tape.Index;
        return ret;
    }
}
