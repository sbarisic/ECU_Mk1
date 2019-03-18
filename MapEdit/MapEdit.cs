using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using unvell.ReoGrid;
using unvell.ReoGrid.Graphics;

namespace MapEdit {
	public partial class MapEdit : Form {
		EditableData CurrentEdited;

		public MapEdit() {
			InitializeComponent();
		}

		private void MapEdit_Load(object sender, EventArgs e) {
			Properties.PropertySort = PropertySort.Categorized;

			Grid.DisableSettings(WorkbookSettings.View_ShowSheetTabControl);
			Grid.Click += GridOnClick;

			Edit(null);
			Tree.AfterSelect += TreeSelected;
		}

		private void GridOnClick(object Sender, EventArgs E) {
			ColorSheet(Grid.CurrentWorksheet);
		}

		void ColorSheet(Worksheet Sheet) {
			if (Sheet == null)
				return;

			for (int y = 0; y < Sheet.ColumnCount; y++)
				for (int x = 0; x < Sheet.RowCount; x++) {
					Cell CurCell = Sheet.Cells[x, y];
					CurrentEdited.ColorCell(x, y, CurCell.Data, ref CurCell);
				}
		}

		public void SetEditable(EditableData[] DataSet) {
			Tree.Nodes.Clear();

			foreach (EditableData ED in DataSet) {
				Type EDType = ED.GetType();

				DesignerCategoryAttribute Cat = EDType.GetCustomAttribute<DesignerCategoryAttribute>();
				DisplayNameAttribute DispName = EDType.GetCustomAttribute<DisplayNameAttribute>() ?? new DisplayNameAttribute(EDType.Name);

				TreeNode CatNode = FindOrCreateCategory(Cat.Category);
				CatNode.Tag = null;

				TreeNode EditNode = CatNode.Nodes.Add(DispName.DisplayName);
				EditNode.Tag = ED;
			}

			Tree.ExpandAll();
		}

		TreeNode FindOrCreateCategory(string Name) {
			foreach (TreeNode N in Tree.Nodes)
				if (N.Text == Name)
					return N;

			return Tree.Nodes.Add(Name);
		}

		void TreeSelected(object Sender, TreeViewEventArgs E) {
			if (E.Node.Tag is EditableData ED)
				Edit(ED);
		}

		void Edit(EditableData Data) {
			CurrentEdited = Data;
			GridPanel.Dock = DockStyle.Fill;
			GridPanel.Visible = false;

			PropertyPanel.Dock = DockStyle.Fill;
			PropertyPanel.Visible = false;

			if (Data == null)
				return;

			if (!Data.DataEnabled)
				return;

			XAxisLabel.Text = Data.XName;
			YAxisLabel.NewText = Data.YName;
			ValueLabel.Text = Data.ValueName;

			switch (Data.EditMode) {
				case EditMode.Grid: {
						GridPanel.Visible = true;
						Grid.Worksheets.Clear();

						if (Data.Worksheet == null) {
							Worksheet WSheet = Grid.Worksheets.Create(string.Format("{0} / {1}", Data.XName, Data.YName));
							Data.Worksheet = WSheet;

							WSheet.RowCount = 1;
							WSheet.ColumnCount = 1;
							Data.PopulateSheet(WSheet);
						}

						Grid.CurrentWorksheet = Data.Worksheet;
						ColorSheet(Data.Worksheet);
						break;
					}

				case EditMode.Property: {
						PropertyPanel.Visible = true;
						Properties.SelectedObject = Data;
						break;
					}

				default:
					throw new Exception("Invalid edit mode " + Data.EditMode);
			}
		}
	}
}
