using ClosedXML.Excel;

namespace Cadence.Core.Features.ExcelExport.Builders;

/// <summary>
/// Builds the reference and template worksheets: Instructions, Lookups, and data validation rules.
/// </summary>
internal static class ExcelTemplateBuilder
{
    /// <summary>
    /// Adds an "Instructions" worksheet as a user guide for filling in the MSEL template.
    /// </summary>
    /// <param name="workbook">The workbook to add the worksheet to.</param>
    /// <param name="includeFormatting">Whether to apply header styling.</param>
    internal static void AddInstructionsWorksheet(XLWorkbook workbook, bool includeFormatting)
    {
        var ws = workbook.Worksheets.Add("Instructions");

        var instructions = new[]
        {
            ("Overview", "This template helps you create a Master Scenario Events List (MSEL) for import into Cadence."),
            ("", ""),
            ("How to Use", ""),
            ("", "1. Fill in the MSEL worksheet with your inject data"),
            ("", "2. Use the Lookups worksheet to see valid values for dropdown fields"),
            ("", "3. The example row (highlighted yellow) can be deleted or replaced"),
            ("", "4. Save as .xlsx and import into Cadence"),
            ("", ""),
            ("Required Fields", "The following fields are required for each inject:"),
            ("", "• Inject # - Unique number for the inject"),
            ("", "• Title - Short descriptive title"),
            ("", "• Description - Full inject content"),
            ("", "• Scheduled Time - When to deliver (HH:mm format)"),
            ("", "• To / Target - Who receives the inject"),
            ("", ""),
            ("Optional Fields", "The following fields are optional:"),
            ("", "• Scenario Day - Day number in multi-day exercises"),
            ("", "• Scenario Time - Time in the exercise scenario"),
            ("", "• From / Source - Who sends the inject"),
            ("", "• Delivery Method - How the inject is delivered (see Lookups)"),
            ("", "• Track - Functional area or team"),
            ("", "• Phase - Exercise phase name"),
            ("", "• Expected Action - What players should do"),
            ("", "• Notes - Controller notes"),
            ("", "• Priority - 1 (highest) to 5 (lowest)"),
            ("", "• Location - Where the inject occurs"),
            ("", "• Responsible Controller - Who fires this inject"),
            ("", ""),
            ("Tips", ""),
            ("", "• Use consistent time formats (HH:mm)"),
            ("", "• Delivery Method values must match the Lookups exactly"),
            ("", "• Leave cells blank for optional fields you don't need"),
            ("", "• Import will validate and report any errors"),
        };

        var row = 1;
        foreach (var (header, content) in instructions)
        {
            if (!string.IsNullOrEmpty(header))
            {
                var headerCell = ws.Cell(row, 1);
                headerCell.Value = header;
                if (includeFormatting)
                {
                    headerCell.Style.Font.Bold = true;
                    headerCell.Style.Font.FontSize = 12;
                }
            }

            if (!string.IsNullOrEmpty(content))
            {
                ws.Cell(row, string.IsNullOrEmpty(header) ? 1 : 2).Value = content;
            }

            row++;
        }

        if (includeFormatting)
        {
            ws.Column(1).Width = 20;
            ws.Column(2).Width = 80;
        }
    }

    /// <summary>
    /// Adds a "Lookups" worksheet containing valid values for dropdown fields (delivery methods,
    /// priorities, inject types, and statuses) and registers named ranges used by data validation.
    /// </summary>
    /// <param name="workbook">The workbook to add the worksheet to.</param>
    /// <param name="includeFormatting">Whether to apply header styling and column widths.</param>
    internal static void AddLookupsWorksheet(XLWorkbook workbook, bool includeFormatting)
    {
        var ws = workbook.Worksheets.Add("Lookups");

        // Delivery Methods - populate the actual values for display and named range
        var deliveryMethods = new[] { "Verbal", "Phone", "Email", "Radio", "Written", "Simulation", "Other" };
        ws.Cell(1, 1).Value = "Delivery Methods";
        if (includeFormatting)
        {
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }
        for (int i = 0; i < deliveryMethods.Length; i++)
        {
            ws.Cell(i + 2, 1).Value = deliveryMethods[i];
        }

        // Priority values (as numbers to avoid "Number stored as text" warning)
        var priorities = new[] { 1, 2, 3, 4, 5 };
        ws.Cell(1, 2).Value = "Priority";
        if (includeFormatting)
        {
            ws.Cell(1, 2).Style.Font.Bold = true;
            ws.Cell(1, 2).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }
        for (int i = 0; i < priorities.Length; i++)
        {
            ws.Cell(i + 2, 2).Value = priorities[i];
        }

        // Inject Types (for reference)
        var injectTypes = new[] { "Standard", "Contingency", "Adaptive", "Complexity" };
        ws.Cell(1, 3).Value = "Inject Types";
        if (includeFormatting)
        {
            ws.Cell(1, 3).Style.Font.Bold = true;
            ws.Cell(1, 3).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }
        for (int i = 0; i < injectTypes.Length; i++)
        {
            ws.Cell(i + 2, 3).Value = injectTypes[i];
        }

        // Inject Statuses (for reference, used in conduct exports)
        var injectStatuses = new[] { "Draft", "Synchronized", "Released", "Deferred" };
        ws.Cell(1, 4).Value = "Inject Status";
        if (includeFormatting)
        {
            ws.Cell(1, 4).Style.Font.Bold = true;
            ws.Cell(1, 4).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }
        for (int i = 0; i < injectStatuses.Length; i++)
        {
            ws.Cell(i + 2, 4).Value = injectStatuses[i];
        }

        if (includeFormatting)
        {
            ws.Column(1).Width = 18;
            ws.Column(2).Width = 10;
            ws.Column(3).Width = 15;
            ws.Column(4).Width = 13;
        }

        // Define named ranges for data validation
        // Use the actual data rows only - Excel data validation with list references
        // correctly validates user input against these values regardless of which row
        // the user is editing in the MSEL worksheet
        var deliveryRange = ws.Range(2, 1, deliveryMethods.Length + 1, 1);
        deliveryRange.AddToNamed("DeliveryMethods", XLScope.Workbook);

        var priorityRange = ws.Range(2, 2, priorities.Length + 1, 2);
        priorityRange.AddToNamed("Priorities", XLScope.Workbook);
    }

    /// <summary>
    /// Applies Excel data validation rules to the Delivery Method and Priority columns
    /// on the MSEL worksheet, referencing named ranges from the Lookups worksheet.
    /// </summary>
    /// <param name="ws">The MSEL worksheet to apply validation to.</param>
    /// <param name="columns">Column definitions used to locate the target columns by field name.</param>
    internal static void AddDataValidation(
        IXLWorksheet ws,
        (string Field, string Header, int Width)[] columns)
    {
        // Find the Delivery Method column index
        var deliveryMethodIndex = -1;
        var priorityIndex = -1;

        for (int i = 0; i < columns.Length; i++)
        {
            if (columns[i].Field == "DeliveryMethod")
            {
                deliveryMethodIndex = i + 1; // 1-based
            }
            else if (columns[i].Field == "Priority")
            {
                priorityIndex = i + 1; // 1-based
            }
        }

        // Apply validation to Delivery Method column (rows 2-1000)
        if (deliveryMethodIndex > 0)
        {
            var deliveryValidation = ws.Range(2, deliveryMethodIndex, 1000, deliveryMethodIndex)
                .CreateDataValidation();
            deliveryValidation.List("=DeliveryMethods", true);
            deliveryValidation.IgnoreBlanks = true;
            deliveryValidation.ShowErrorMessage = true;
            deliveryValidation.ErrorTitle = "Invalid Delivery Method";
            deliveryValidation.ErrorMessage = "Please select a delivery method from the dropdown list.";
        }

        // Apply validation to Priority column (rows 2-1000)
        if (priorityIndex > 0)
        {
            var priorityValidation = ws.Range(2, priorityIndex, 1000, priorityIndex)
                .CreateDataValidation();
            priorityValidation.List("=Priorities", true);
            priorityValidation.IgnoreBlanks = true;
            priorityValidation.ShowErrorMessage = true;
            priorityValidation.ErrorTitle = "Invalid Priority";
            priorityValidation.ErrorMessage = "Please select a priority value (1-5) from the dropdown list.";
        }
    }
}
