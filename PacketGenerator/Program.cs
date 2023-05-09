using PacketGenerator;
using System.Xml;

XmlReaderSettings _setting = new XmlReaderSettings()
{
    IgnoreComments = true,
    IgnoreWhitespace = true,
};

string generatePackets = "";

using (XmlReader r = XmlReader.Create("PDL.xml", settings: _setting))
{
    r.MoveToContent();

    while (r.Read())
    {
        if (r.Depth == 1) ParsePacket(r);
    }

    File.WriteAllText("GeneratePacket.cs", generatePackets);
}

void ParsePacket(XmlReader r)
{
    if (r.NodeType != XmlNodeType.Element || r.Name.ToLower() != "packet") return;

    string packetName = r["name"];
    if (string.IsNullOrEmpty(packetName))
    {
        Console.WriteLine("패킷 이름이 비어있음");
        return;
    }

    var parseTuple = ParseMembers(r);
    generatePackets += string.Format(PacketFormat.packetFormat, packetName, parseTuple.Item1, parseTuple.Item2, parseTuple.Item3);
}

Tuple<string, string, string> ParseMembers(XmlReader r)
{
    int targetDepth = r.Depth + 1;
    string memberCode = "";
    string writeCode = "";
    string readCode = "";

    while (r.Read())
    {
        if (r.Depth != targetDepth) break;
        if (string.IsNullOrEmpty(r["name"]))
        {
            Console.WriteLine("멤버 이름이 비어있음");
            break;
        }

        memberCode = AddLine(memberCode);
        writeCode = AddLine(writeCode);
        readCode = AddLine(readCode);

        string memberType = r.Name.ToLower();
        string memberName = r["name"];
        switch (memberType)
        {
            case "bool":
            case "int":
            case "short":
            case "ushort":
            case "long":
            case "float":
            case "byte":
                memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
                writeCode += string.Format(PacketFormat.writeFormat, memberName, memberType);
                readCode += string.Format(PacketFormat.readFormat, memberName, ToMemberReadFunction(memberType), memberType);
                break;
            case "string":
                memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
                writeCode += string.Format(PacketFormat.writeStringFormat, memberName);
                readCode += string.Format(PacketFormat.readStringFormat, memberName);
                break;
            case "list":
                break;
            default: break;
        }
    }

    memberCode = memberCode.Replace("\n", "\t");
    writeCode = writeCode.Replace("\n", "\t\t");
    readCode = readCode.Replace("\n", "\t\t");
    return new Tuple<string, string, string>(memberCode, writeCode, readCode);
}

string AddLine(string text) => text += (string.IsNullOrEmpty(text)) ? "" : Environment.NewLine;

string ToMemberReadFunction(string memberType)
{
    switch (memberType)
    {
        case "bool":
            return "ToBoolean";
        case "int":
            return "ToInt32";
        case "short":
            return "ToInt16";
        case "ushort":
            return "ToUInt16";
        case "long":
            return "ToInt64";
        case "float":
            return "ToSingle";
        case "byte":
            return "";
        default : return "";
    }
}