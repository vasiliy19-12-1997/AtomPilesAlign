#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

#endregion

namespace AtomPilesAlign
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements
        )
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // ����� ������������ �����
                var selection = uidoc.Selection;
                var pickedref = selection.PickObject(ObjectType.Element, "�������� ���������");
                var elem = doc.GetElement(pickedref);
                var floor = elem as Floor;
                if (floor == null)
                {
                    message = "��������� ������� �� �������� �����������.";
                    return Result.Failed;
                }

                // ��������� ������ ������� � ������� �����
                var floorBoundingBox = floor.get_BoundingBox(null);
                double floorBottom = floorBoundingBox.Min.Z;

                // ����� ����
                var pileIds = selection
                    .PickObjects(ObjectType.Element, "�������� ��������� ����")
                    ?.Select(x => x.ElementId);
                if (pileIds == null || !pileIds.Any())
                {
                    message = "�� ������� �� ����� ����.";
                    return Result.Failed;
                }
                var piles = pileIds.Select(id => doc.GetElement(id)).OfType<FamilyInstance>();

                using (Transaction trans = new Transaction(doc, "��������� ����"))
                {
                    trans.Start();
                    foreach (var pile in piles)
                    {
                        // ������������� �������� ����� �������� ����
                        var param = pile.get_Parameter(
                            BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM
                        );
                        if (param != null && !param.IsReadOnly)
                        {
                            param.Set(floorBottom);
                        }
                    }
                    trans.Commit();
                }
                TaskDialog.Show(
                    "�����",
                    "���� ������� ��������� �� ������ ������� ������������ �����."
                );
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = "������: " + ex.Message;
                return Result.Failed;
            }
        }
    }
}
