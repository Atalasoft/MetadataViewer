using System;
using System.Windows.Forms;
using System.Xml;
using Atalasoft.Imaging.Metadata;

namespace MetadataViewer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    ClearMetadata();
                    LoadMetadata(openFileDialog.FileName);
                }
            }
        }

        private void ClearMetadata()
        {
            SetListViewText(listViewIPTC, "none");

            SetTreeViewText(treeViewXMP, "none");
        }

        private void SetListViewText(ListView listView, string text)
        {
            listView.Groups.Clear();
            listView.Items.Add(text);
        }

        private void SetTreeViewText(TreeView treeView, string text)
        {
            treeView.Nodes.Clear();
            treeView.Nodes.Add(text);
        }

        private void LoadMetadata(string fileName)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;

                ReadIPTC(fileName, listViewIPTC);
                ReadXMP(fileName, treeViewXMP);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void ReadXMP(string fileName, TreeView treeView)
        {
            try
            {
                treeView.Nodes.Clear();

                XmpParser parser = new XmpParser();
                var document = (System.Xml.XmlDocument) parser.ParseFromImage(fileName);

                ConvertXmlNodeToTreeNode(document, treeView.Nodes);
                treeView.Nodes[0].ExpandAll();
            }
            catch (Exception ex)
            {
                SetTreeViewText(treeView, string.Format("Failed to read XMP metadata. Error: {0}", ex.Message));
            }
        }

        // the code that converts xml into TreeNodeCollection is taken from stackoverflow: https://stackoverflow.com/questions/6582836/how-to-show-xml-data-in-the-winform-in-the-xml-fashion
        private void ConvertXmlNodeToTreeNode(XmlNode xmlNode, TreeNodeCollection treeNodes)
        {
            if (xmlNode == null) return;

            TreeNode newTreeNode = treeNodes.Add(xmlNode.Name);

            switch (xmlNode.NodeType)
            {
                case XmlNodeType.ProcessingInstruction:
                case XmlNodeType.XmlDeclaration:
                    newTreeNode.Text = "<?" + xmlNode.Name + " " +
                                       xmlNode.Value + "?>";
                    break;
                case XmlNodeType.Element:
                    newTreeNode.Text = "<" + xmlNode.Name + ">";
                    break;
                case XmlNodeType.Attribute:
                    newTreeNode.Text = "ATTRIBUTE: " + xmlNode.Name;
                    break;
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                    newTreeNode.Text = xmlNode.Value;
                    break;
                case XmlNodeType.Comment:
                    newTreeNode.Text = "<!--" + xmlNode.Value + "-->";
                    break;
            }

            if (xmlNode.Attributes != null)
            {
                foreach (XmlAttribute attribute in xmlNode.Attributes)
                {
                    ConvertXmlNodeToTreeNode(attribute, newTreeNode.Nodes);
                }
            }
            foreach (XmlNode childNode in xmlNode.ChildNodes)
            {
                ConvertXmlNodeToTreeNode(childNode, newTreeNode.Nodes);
            }
        }

        private void ReadIPTC(string fileName, ListView listView)
        {
            try
            {
                IptcParser parser = new IptcParser();
                IptcCollection tags = parser.ParseFromImage(fileName);

                listView.Groups.Clear();
                listView.Items.Clear();
                listView.View = View.Details;

                foreach (IptcTag iptcTag in tags)
                {
                    ListViewGroup group = null;
                    for (int i = 0; i < listView.Groups.Count; i++)
                    {
                        if ((int) listView.Groups[i].Tag == iptcTag.Section)
                        {
                            group = listView.Groups[i];
                            break;
                        }
                    }

                    if (group == null)
                    {
                        group = new ListViewGroup(string.Format("Section: {0}", iptcTag.Section));
                        group.Tag = iptcTag.Section;
                        listView.Groups.Add(group);
                    }

                    listView.Items.Add(new ListViewItem { Text = string.Format("{0}: {1}", iptcTag.ID, iptcTag.Data), Group = group});
                }
            }
            catch (Exception ex)
            {
                SetListViewText(listView, string.Format("Failed to read IPTC metadata. Error: {0}", ex.Message));
            }
        }
    }
}
