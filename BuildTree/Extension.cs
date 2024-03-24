using System.Xml;
using System.Xml.Serialization;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace BuildTree;

public static class Extension
{
    public static void ToXml(this Tree tree, XmlDocument document)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(Tree), new XmlRootAttribute("Tree"));

        using var stream = new MemoryStream();
        serializer.Serialize(stream, tree);
        stream.Position = 0;
        document.Load(stream);
    }

    public static Tree ToTree(this XmlDocument document)
    {
        if (document.DocumentElement == null)
            throw new ArgumentException("The provided XmlDocument does not have a root element.");

        XmlSerializer serializer = new XmlSerializer(typeof(Tree), new XmlRootAttribute("Tree"));

        using var stringWriter = new StringWriter();
        document.Save(stringWriter);
        string xmlString = stringWriter.ToString();

        using var stringReader = new StringReader(xmlString);
        var deserializedTree = (Tree?)serializer.Deserialize(stringReader);
        if (deserializedTree == null)
            throw new ArgumentException("The provided XmlDocument does not have a root element.");
        deserializedTree.FromXml();
        return deserializedTree;
    }

    public static void RemoveLast<T>(this List<T> list)
    {
        if (list is null)
        {
            throw new ArgumentNullException(nameof(list));
        }

        if (list.Count > 0)
        {
            list.RemoveAt(list.Count - 1);
        }
    }

    public static void AddOrReplaceLast<T>(this List<T> list, T item)
    {
        if (list is null)
        {
            throw new ArgumentNullException(nameof(list));
        }

        if (list.Count == 0)
        {
            list.Add(item);
        }
        else
        {
            list[list.Count - 1] = item;
        }
    }

    public static IEnumerable<(int Index, bool First, bool Last, T Item)> Enumerate<T>(this IEnumerable<T> source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return Enumerate(source.GetEnumerator());
    }

    public static IEnumerable<(int Index, bool First, bool Last, T Item)> Enumerate<T>(this IEnumerator<T> source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var first = true;
        var last = !source.MoveNext();
        T current;

        for (var index = 0; !last; index++)
        {
            current = source.Current;
            last = !source.MoveNext();
            yield return (index, first, last, current);
            first = false;
        }
    }

}
