using System.Xml.Serialization;
using Spectre.Console;
using Spectre.Console.Rendering;

// Copy Past of the spectre tree, but with the _root exposed and it Display Text
namespace BuildTree;


public interface ITreeNode
{
    List<TreeNode> Nodes { get; }
}

/// <summary>
/// Representation of non-circular tree data.
/// Each node added to the tree may only be present in it a single time, in order to facilitate cycle detection.
/// </summary>
public sealed class Tree : Renderable, ITreeNode
{
    /// <summary>
    /// Gets the tree root.
    /// </summary>
    public TreeNode Root { get; set; }

    /// <summary>
    /// Gets or sets the tree style.
    /// </summary>
    [XmlIgnore]
    public Style? Style { get; set; }

    /// <summary>
    ///  Gets or sets the tree guide lines.
    /// </summary>
    [XmlIgnore]
    public TreeGuide Guide { get; set; } = TreeGuide.Line;

    /// <summary>
    /// Gets the tree's child nodes.
    /// </summary>
    [XmlIgnore]
    public List<TreeNode> Nodes => Root.Nodes;

    /// <summary>
    /// Gets or sets a value indicating whether or not the tree is expanded or not.
    /// </summary>
    [XmlIgnore]
    public bool Expanded { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tree"/> class.
    /// </summary>
    /// <param name="label">The tree label.</param>
    public Tree(string label)
    {
        Root = new TreeNode(label, null);
    }

    public Tree()
    {
        Root = new TreeNode("Node", null);
    }

    /// <summary>
    /// Adds a node to the tree.
    /// </summary>
    /// <param name="text">The text added as a node.</param>
    public void AddNode(string text)
    {
        Root.Nodes.Add(new TreeNode(text, Root));
    }

    /// <inheritdoc />
    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var result = new List<Segment>();
        var visitedNodes = new HashSet<TreeNode>();

        var stack = new Stack<Queue<TreeNode>>();
        stack.Push(new Queue<TreeNode>(new[] { Root }));

        var levels = new List<Segment>();
        levels.Add(GetGuide(options, TreeGuidePart.Continue));

        while (stack.Count > 0)
        {
            var stackNode = stack.Pop();
            if (stackNode.Count == 0)
            {
                levels.RemoveLast();
                if (levels.Count > 0)
                {
                    levels.AddOrReplaceLast(GetGuide(options, TreeGuidePart.Fork));
                }

                continue;
            }

            var isLastChild = stackNode.Count == 1;
            var current = stackNode.Dequeue();
            if (!visitedNodes.Add(current))
            {
                throw new Exception("Cycle detected in tree - unable to render.");
            }

            stack.Push(stackNode);

            if (isLastChild)
            {
                levels.AddOrReplaceLast(GetGuide(options, TreeGuidePart.End));
            }

            var prefix = levels.Skip(1).ToList();
            var renderable = (IRenderable)current.Label;
            var renderableLines = Segment.SplitLines(renderable.Render(options, maxWidth - Segment.CellCount(prefix)));

            foreach (var (_, isFirstLine, _, line) in renderableLines.Enumerate())
            {
                if (prefix.Count > 0)
                {
                    result.AddRange(prefix.ToList());
                }

                result.AddRange(line);
                result.Add(Segment.LineBreak);

                if (isFirstLine && prefix.Count > 0)
                {
                    var part = isLastChild ? TreeGuidePart.Space : TreeGuidePart.Continue;
                    prefix.AddOrReplaceLast(GetGuide(options, part));
                }
            }

            if (current.Expanded && current.Nodes.Count > 0)
            {
                levels.AddOrReplaceLast(GetGuide(options, isLastChild ? TreeGuidePart.Space : TreeGuidePart.Continue));
                levels.Add(GetGuide(options, current.Nodes.Count == 1 ? TreeGuidePart.End : TreeGuidePart.Fork));

                stack.Push(new Queue<TreeNode>(current.Nodes));
            }
        }

        return result;
    }

    private Segment GetGuide(RenderOptions options, TreeGuidePart part)
    {
        var guide = Guide.GetSafeTreeGuide(safe: !options.Unicode);
        return new Segment(guide.GetPart(part), Style ?? Style.Plain);
    }

    internal void FromXml()
        => Root.FromXml();
}

/// <summary>
/// Represents a tree node.
/// </summary>
public sealed class TreeNode : ITreeNode
{
    string rawText;

    /// <summary>
    /// Gets the text of the label with out be parsed.
    /// </summary>
    public string RawText 
    { 
        get => rawText;
        set => UpdateText(value); 
    }

    /// <summary>
    /// Gets the tree node label.
    /// </summary>
    [XmlIgnore]
    public Markup Label { get; private set; }

    /// <summary>
    /// Gets the tree node's child nodes.
    /// </summary>
    public List<TreeNode> Nodes { get; } = new List<TreeNode>();

    /// <summary>
    /// Gets the parent of the tree node.
    /// Null if the root node.
    /// </summary>
    [XmlIgnore]
    public TreeNode? Parent { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not the tree node is expanded or not.
    /// </summary>
    [XmlIgnore]
    public bool Expanded { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="TreeNode"/> class.
    /// </summary>
    /// <param name="label">The tree node label.</param>
    public TreeNode(string label, TreeNode? parent)
    {
        UpdateText(label);
        Parent = parent;
    }

    public TreeNode()
    {
        UpdateText(string.Empty);
        Nodes = new List<TreeNode>();
    }

    public void UpdateText(string newText)
    {
        rawText = newText;
        Label = new Markup(newText);
    }

    /// <summary>
    /// Adds a node to this node.
    /// </summary>
    /// <param name="text">The text added as a node.</param>
    public void AddNode(string text)
    {
        Nodes.Add(new TreeNode(text, this));
    }

    public override string ToString() => RawText;

    internal void FromXml()
    {
        Label = new Markup(RawText);
        foreach (var node in Nodes)
        {
            node.Parent = this;
            node.FromXml();
        }
    }
}