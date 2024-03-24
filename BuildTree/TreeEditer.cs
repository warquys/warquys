using System.Xml;
using Spectre.Console;

namespace BuildTree;

public class TreeEditer
{
    // Do not allow edition of the Tree to avoid conflit with seralization
    private Tree tree;
    private XmlDocument document;

    public string FilePath { get; }
   
    public bool LastEditionSaved { get; private set; }
    public bool Editing { get; private set; }

    public TreeEditer(string loadedFile)
    {
        FilePath = loadedFile;
        document = new XmlDocument();
        document.Load(FilePath);
        tree = document.ToTree();
    }

    public TreeEditer(string root, string filePath)
    {
        tree = new Tree(root);
        document = new XmlDocument();
        FilePath = filePath;
        Save();
    }

    public void StartEdition()
    {
        Editing = true;
        AnsiConsole.AlternateScreen(() =>
        {
            while (Editing)
            {
                AnsiConsole.Clear();
                AnsiConsole.Write(tree);
                Do(AskAction());
            }
        });
    }

    public EditionChoices AskAction()
    {
        AnsiConsole.WriteLine();
        var choice = 
            new SelectionPrompt<EditionChoices>()
                .Title("What do you want to do ?")
                .AddChoices(new[] { 
                    EditionChoices.Add,
                    EditionChoices.Rename,
                    EditionChoices.Exit });
        
        if (tree.Nodes.Count > 0)
            choice.AddChoices(EditionChoices.Remove);
        
        if (!LastEditionSaved)
            choice.AddChoices(EditionChoices.Save);

        return AnsiConsole.Prompt(choice);
    }

    public void Do(EditionChoices action)
    {
        switch (action)
        {
            case EditionChoices.Add:
                AskAdd();
                break;
            case EditionChoices.Remove:
                AskRemove();
                break;
            case EditionChoices.Rename:
                AskRename();
                break;
            case EditionChoices.Save:
                Save();
                break;
            case EditionChoices.Exit:
                AskExit();
                break;
        }
    }

    private void AskExit()
    {
        if (!LastEditionSaved && AnsiConsole.Confirm("Do you want to save before exit ?"))
        {
            Save();
        }

        Editing = false;
    }

    private void Save()
    {
        LastEditionSaved = true;
        tree.ToXml(document);
        document.Save(FilePath);
    }

    private void AskRename()
    {
        LastEditionSaved = false;
        var editedNode = SelectTreeNodes(true);
        if (editedNode.Count == 0) return;

        var askName = new TextPrompt<string>($"What is the new name for this node{AddPuriel(editedNode)}? (empty to abort)")
            .AllowEmpty();
        var name = AnsiConsole.Prompt(askName);
        if (string.IsNullOrWhiteSpace(name)) return;
        editedNode.ForEach(p => p.Node.UpdateText(name));
    }

    private void AskRemove()
    {
        LastEditionSaved = false;
        var removeNodes = SelectTreeNodes(false);
        if (removeNodes.Count == 0) return;

        if (AnsiConsole.Confirm($"Do you want to remove this node{AddPuriel(removeNodes)}", false))
            removeNodes.ForEach(p => p.Node.Parent?.Nodes.Remove(p.Node));
    }

    private void AskAdd()
    {
        LastEditionSaved = false;
        var parentOfAddedNodes = SelectTreeNodes(true);
        if (parentOfAddedNodes.Count == 0) return;

        var askName = new TextPrompt<string>($"What is the name of the new node{AddPuriel(parentOfAddedNodes)}? (empty to abort)")
            .AllowEmpty();
        var name = AnsiConsole.Prompt(askName);
        if (string.IsNullOrWhiteSpace(name)) return;
        parentOfAddedNodes.ForEach(p => p.Node.AddNode(name));
    }

    private string AddPuriel<T>(List<T> list)
        => list.Count > 1 ? "s" : "";

    // Is called "branch" a ducplicated of a node with childs to allow the user to select the childs node
    private SelectNode? SelectTreeNode(bool rootSlectable)
    {
        AnsiConsole.Clear();
        var selecte = new SelectionPrompt<SelectNode>()
        {
            PageSize = 20
        };
        var branchs = new Stack<Branchs>();

        if (tree.Root.Nodes.Count == 0 && !rootSlectable) return null;

        if (rootSlectable)
        {
            selecte.AddChoice(new SelectNode(tree.Root) { IsBranch = true });
        }

        if (tree.Nodes.Count > 0)
        {
            var rootAsBranch = selecte.AddChoice(new SelectNode(tree.Root) { IsBranch = true }) ;
            branchs.Push(new Branchs(new Queue<TreeNode>(tree.Nodes), rootAsBranch));
            while (branchs.TryPeek(out var branch))
            {
                if (!branch.AveChilds)
                {
                    branchs.Pop();
                    continue;
                }

                branch.Deconstruct(out var childs, out var curent);

                var currentChild = childs.Dequeue();
                curent.AddChild(new SelectNode(currentChild));

                if (currentChild.Nodes.Count > 0)
                {
                    var childAsBranch = curent.AddChild(new SelectNode(currentChild) { IsBranch = true });
                    branchs.Push(new Branchs(new Queue<TreeNode>(currentChild.Nodes), childAsBranch));
                }
            }
        }

        return AnsiConsole.Prompt(selecte);
    }

    private List<SelectNode> SelectTreeNodes(bool rootSlectable)
    {
        AnsiConsole.Clear();
        var selectes = new MultiSelectionPrompt<SelectNode>()
        {
            PageSize = 20
        };
        var branchs = new Stack<Branchs>();

        if (tree.Root.Nodes.Count == 0 && !rootSlectable) return [];

        if (rootSlectable)
        {
            selectes.AddChoice(new SelectNode(tree.Root) { IsBranch = true });
        }

        if (tree.Nodes.Count > 0)
        {
            var rootAsBranch = selectes.AddChoice(new SelectNode(tree.Root) { IsBranch = true }) ;
            branchs.Push(new Branchs(new Queue<TreeNode>(tree.Nodes), rootAsBranch));
            while (branchs.TryPeek(out var branch))
            {
                if (!branch.AveChilds)
                {
                    branchs.Pop();
                    continue;
                }

                branch.Deconstruct(out var childs, out var curent);

                var currentChild = childs.Dequeue();
                curent.AddChild(new SelectNode(currentChild));

                if (currentChild.Nodes.Count > 0)
                {
                    var childAsBranch = curent.AddChild(new SelectNode(currentChild) { IsBranch = true });
                    branchs.Push(new Branchs(new Queue<TreeNode>(currentChild.Nodes), childAsBranch));
                }
            }
        }

        return AnsiConsole.Prompt(selectes);
    }

    // Use to build the tree
    public record class Branchs(Queue<TreeNode> Childs, ISelectionItem<SelectNode> Curent)
    {
        public bool AveChilds => Childs.Count > 0;
    }

    // Use to ref the tree node
    public record class SelectNode(TreeNode Node)
    {
        public bool IsRoot { get; set; } = false;

        public bool IsBranch { get; set; } = false;

        public override string ToString() => Node.ToString();
    }
}
