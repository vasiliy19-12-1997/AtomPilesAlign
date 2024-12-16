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
                // Выбор фундаментной плиты
                var selection = uidoc.Selection;
                var pickedref = selection.PickObject(ObjectType.Element, "Выберите фундамент");
                var elem = doc.GetElement(pickedref);
                var floor = elem as Floor;
                if (floor == null)
                {
                    message = "Выбранный элемент не является фундаментом.";
                    return Result.Failed;
                }

                // Получение нижней границы и толщины плиты
                var floorBoundingBox = floor.get_BoundingBox(null);
                double floorBottom = floorBoundingBox.Min.Z;

                // Выбор свай
                var pileIds = selection
                    .PickObjects(ObjectType.Element, "Выберите несколько свай")
                    ?.Select(x => x.ElementId);
                if (pileIds == null || !pileIds.Any())
                {
                    message = "Не выбрано ни одной сваи.";
                    return Result.Failed;
                }
                var piles = pileIds.Select(id => doc.GetElement(id)).OfType<FamilyInstance>();

                using (Transaction trans = new Transaction(doc, "Выровнять сваи"))
                {
                    trans.Start();
                    foreach (var pile in piles)
                    {
                        // Устанавливаем смещение через параметр сваи
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
                    "Успех",
                    "Сваи успешно выровнены по нижней границе фундаментной плиты."
                );
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = "Ошибка: " + ex.Message;
                return Result.Failed;
            }
        }
    }
}
