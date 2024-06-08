namespace Sugarmaple.TheSeed.Namumark;

using Sugarmaple.TheSeed.Namumark.Parsing;
using System.Diagnostics;

public abstract class Macro : Clause
{
    private string? _argument;
    public string? Argument
    {
        get
        {
            if (!_isArgumentValid)
            {
                _argument = SeekArgument();
                _isArgumentValid = true;
            }
            return _argument;
        }
        set
        {
            _argument = value;
            _isArgumentValid = true;
        }
    } //@@ 문법 치환이 가능한 string
    private bool _isArgumentValid; //Argument가 유효한 값인지 표기합니다.

    /// <summary>
    /// <see cref="_isArgumentValid"/>가 false일 때, <see cref="Argument"/>에서 호출됩니다.
    /// </summary>
    /// <returns>직렬화된 Argument. 하위 클래스에서 구현되지 않으면 null을 반환합니다.</returns>
    protected virtual string? SeekArgument() => null;

    /// <summary>
    /// 추상화된 필드로 값이 입력되어 실제 <see cref="_isArgumentValid"/>를 false로 바꿉니다.
    /// </summary>
    protected void InvalidateArgument()
    {
        _isArgumentValid = false;
        NotifyChange();
    }

    private protected Macro()
    {

    }
}

public class BrMacro : Macro
{
}

public class TableOfContents : Macro
{
}

public class Include : Macro, IReferer
{
    private string? _reference;
    private bool _isReferenceValid;

    public Include()
    {

    }

    public Include(string reference, Dictionary<string, string> arguments)
    {
        Reference = reference;
        Arguments = arguments;
    }

    //argument로부터 도출
    //get =>
    //  1. 파싱을 진행한 뒤 임시 필드로서 보관함.
    //set => 
    // argument = null
    //  argument => get

    public string Reference
    {
        get
        {
            if (!_isReferenceValid)
            {
                ParseMember();
            }
            return _reference ?? "";
        }

        set
        {
            _reference = value;
            InvalidateArgument();
        }
    }

    private void ParseMember()
    {
        var tape = new StringTape(Argument);
        var reference = tape.GetStringToken(',');
        _reference = reference.Trim();
        var args = new Dictionary<string, string>();
        while (!tape.IsEnd)
        {
            tape.Trim();
            var name = tape.GetStringToken('=');
            tape.Trim();
            var value = tape.GetStringToken(',');
            args[name] = value;
        }
        Arguments = args;
        _isReferenceValid = true;
    }

    //IDictionary로 변형이 발생할 때마다 로직 처리.
    public Dictionary<string, string> Arguments { get; private set; }//argument로부터 도출 //miniArgument로부터 도출

    private static string Escape(string str)
    {
        return str.Replace(",", @"\,");
    }
}

public class ObservableDictionary
{

}