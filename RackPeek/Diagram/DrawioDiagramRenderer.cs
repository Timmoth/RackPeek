using System.Text;
using RackPeek.Domain.Diagram;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Diagram;

public class DrawioDiagramRenderer : IDiagramRenderer
{
    public string Render(
        IReadOnlyList<Hardware> hardware,
        IReadOnlyList<SystemResource> systems,
        IReadOnlyList<Service> services)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<mxfile>");
        sb.AppendLine("  <diagram name=\"RackPeek\">");
        sb.AppendLine("    <mxGraphModel>");
        sb.AppendLine("      <root>");
        sb.AppendLine("        <mxCell id=\"0\"/>");
        sb.AppendLine("        <mxCell id=\"1\" parent=\"0\"/>");

        var idCounter = 2;

        int spacing = 40;
        int innerPadding = 20;
        int swimlanePadding = 40;
        int serviceNodeHeight = 70;
        
        int estimatedMaxWidth = hardware.Count > 0 ? 800 : 600;

        
        int hypervisorX = 40;
        int hypervisorY = 40;
        int column = 0;
        int maxColumns = 3;
        int columnWidth = estimatedMaxWidth + 200;

        // Track tallest item in each row
        int rowMaxHeight = 0;

        // VMs = SystemResource where Type == "vm"
        var vms = systems
            .Where(s => s.Type?.Equals("vm", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        // Show ALL hardware (Option A)
        var hypervisors = hardware.ToList();

        foreach (var hypervisor in hypervisors)
        {
            var vmsOnHypervisor = vms
                .Where(vm => vm.RunsOn == hypervisor.Name)
                .ToList();

            var servicesOnHypervisor = services
                .Where(s => s.RunsOn == hypervisor.Name)
                .ToList();

            // ---------- PREPASS: compute widths/heights ----------

            int baremetalGroupHeight = 0;
            int baremetalGroupWidth = 0;
            int baremetalNodeWidth = 0;

            if (servicesOnHypervisor.Any())
            {
                var baremetalLabels = servicesOnHypervisor
                    .Select(BuildServiceLabel)
                    .ToList();

                int maxLabelWidth = baremetalLabels
                    .Select(MeasureLabelWidth)
                    .DefaultIfEmpty(0)
                    .Max();

                baremetalNodeWidth = maxLabelWidth + innerPadding * 2;

                int count = servicesOnHypervisor.Count;
                bool useGrid = count > 3;

                if (useGrid)
                {
                    int columns = GetColumnCount(count);
                    int rows = (int)Math.Ceiling(count / (double)columns);

                    baremetalGroupWidth =
                        swimlanePadding +
                        (columns * baremetalNodeWidth) +
                        ((columns - 1) * spacing) +
                        swimlanePadding;

                    baremetalGroupHeight =
                        swimlanePadding +
                        (rows * serviceNodeHeight) +
                        ((rows - 1) * spacing) +
                        swimlanePadding;
                }
                else
                {
                    baremetalGroupWidth =
                        swimlanePadding +
                        baremetalNodeWidth +
                        swimlanePadding;

                    baremetalGroupHeight =
                        swimlanePadding +
                        (count * serviceNodeHeight) +
                        ((count - 1) * spacing) +
                        swimlanePadding;
                }
            }

            // VMs: compute per-VM width/height
            var vmHeights = new Dictionary<string, int>();
            var vmWidths = new Dictionary<string, int>();

            foreach (var vm in vmsOnHypervisor)
            {
                var servicesOnVm = services.Where(s => s.RunsOn == vm.Name).ToList();

                var serviceGroups = servicesOnVm.GroupBy(s => s.Kind.ToLowerInvariant());

                int vmHeight = swimlanePadding;
                int vmContentWidth = 0;

                foreach (var group in serviceGroups)
                {
                    var groupServices = group.ToList();
                    var labels = groupServices.Select(BuildServiceLabel).ToList();

                    int maxLabelWidth = labels
                        .Select(MeasureLabelWidth)
                        .DefaultIfEmpty(0)
                        .Max();

                    int maxNodeWidth = maxLabelWidth + innerPadding * 2;

                    int serviceCount = groupServices.Count;
                    int columns = GetColumnCount(serviceCount);
                    int rows = (int)Math.Ceiling(serviceCount / (double)columns);

                    int groupWidth =
                        swimlanePadding +
                        (columns * maxNodeWidth) +
                        ((columns - 1) * spacing) +
                        swimlanePadding;

                    int groupHeight =
                        swimlanePadding +
                        (rows * serviceNodeHeight) +
                        ((rows - 1) * spacing) +
                        swimlanePadding;

                    vmHeight += groupHeight + spacing;
                    vmContentWidth = Math.Max(vmContentWidth, groupWidth);
                }

                vmHeight += swimlanePadding;
                int vmWidth = vmContentWidth + swimlanePadding * 2;

                vmHeights[vm.Name] = Math.Max(vmHeight, 120);
                vmWidths[vm.Name] = Math.Max(vmWidth, 200);
            }

            int maxVmWidth = vmWidths.Values.DefaultIfEmpty(0).Max();
            int hypervisorInnerWidth = Math.Max(maxVmWidth, baremetalGroupWidth);
            int hypervisorWidth = hypervisorInnerWidth + swimlanePadding * 2;

            int hypervisorHeight =
                swimlanePadding +
                (servicesOnHypervisor.Any() ? baremetalGroupHeight + spacing : 0) +
                vmHeights.Values.Sum() +
                (vmHeights.Count * spacing) +
                swimlanePadding;

            // Minimum size for empty hardware
            hypervisorWidth = Math.Max(hypervisorWidth, 300);
            hypervisorHeight = Math.Max(hypervisorHeight, 120);

            // Track tallest item in this row
            rowMaxHeight = Math.Max(rowMaxHeight, hypervisorHeight);
            
            columnWidth = Math.Max(columnWidth, hypervisorWidth + 100);

            // ---------- EMIT: hypervisor ----------

            string hypervisorId = (idCounter++).ToString();
            string hypervisorLabel = BuildHypervisorLabel(hypervisor);

            sb.AppendLine(
                $"        <mxCell id=\"{hypervisorId}\" value=\"{Escape(hypervisorLabel)}\" style=\"swimlane;rounded=1;fillColor=#fff2cc;strokeColor=#000000;fontStyle=1\" vertex=\"1\" parent=\"1\">");
            sb.AppendLine(
                $"          <mxGeometry x=\"{hypervisorX}\" y=\"{hypervisorY}\" width=\"{hypervisorWidth}\" height=\"{hypervisorHeight}\" as=\"geometry\"/>");
            sb.AppendLine("        </mxCell>");

            int currentY = swimlanePadding;

            // ---------- EMIT: bare-metal group ----------

            if (servicesOnHypervisor.Any())
            {
                string baremetalGroupId = (idCounter++).ToString();
                string baremetalLabel = "Services: baremetal";

                sb.AppendLine(
                    $"        <mxCell id=\"{baremetalGroupId}\" value=\"{Escape(baremetalLabel)}\" style=\"swimlane;rounded=1;fillColor=#f8cecc;strokeColor=#000000;fontStyle=1\" vertex=\"1\" parent=\"{hypervisorId}\">");
                sb.AppendLine(
                    $"          <mxGeometry x=\"{swimlanePadding}\" y=\"{currentY}\" width=\"{baremetalGroupWidth}\" height=\"{baremetalGroupHeight}\" as=\"geometry\"/>");
                sb.AppendLine("        </mxCell>");

                int count = servicesOnHypervisor.Count;
                bool useGrid = count > 3;
                int columns = useGrid ? GetColumnCount(count) : 1;

                int index = 0;
                for (int i = 0; i < count; i++)
                {
                    var svc = servicesOnHypervisor[i];
                    string svcId = (idCounter++).ToString();
                    string svcLabel = BuildServiceLabel(svc);

                    int col = useGrid ? (index % columns) : 0;
                    int row = useGrid ? (index / columns) : index;

                    int x = swimlanePadding + col * (baremetalNodeWidth + spacing);
                    int y = swimlanePadding + row * (serviceNodeHeight + spacing);

                    sb.AppendLine(
                        $"        <mxCell id=\"{svcId}\" value=\"{Escape(svcLabel)}\" style=\"rounded=1;fillColor=#ffffff;strokeColor=#000000;\" vertex=\"1\" parent=\"{baremetalGroupId}\">");
                    sb.AppendLine(
                        $"          <mxGeometry x=\"{x}\" y=\"{y}\" width=\"{baremetalNodeWidth}\" height=\"{serviceNodeHeight}\" as=\"geometry\"/>");
                    sb.AppendLine("        </mxCell>");

                    index++;
                }

                currentY += baremetalGroupHeight + spacing;
            }

            // ---------- EMIT: VMs ----------

            foreach (var vm in vmsOnHypervisor)
            {
                int vmHeight = vmHeights[vm.Name];
                int vmWidth = vmWidths[vm.Name];

                string vmId = (idCounter++).ToString();

                sb.AppendLine(
                    $"        <mxCell id=\"{vmId}\" value=\"VM: {Escape(vm.Name)}\" style=\"swimlane;rounded=1;fillColor=#dae8fc;strokeColor=#000000;fontStyle=1\" vertex=\"1\" parent=\"{hypervisorId}\">");
                sb.AppendLine(
                    $"          <mxGeometry x=\"{swimlanePadding}\" y=\"{currentY}\" width=\"{vmWidth}\" height=\"{vmHeight}\" as=\"geometry\"/>");
                sb.AppendLine("        </mxCell>");

                var servicesOnVm = services.Where(s => s.RunsOn == vm.Name).ToList();

                var serviceGroups = servicesOnVm.GroupBy(s => s.Kind.ToLowerInvariant());

                int serviceGroupY = swimlanePadding;

                foreach (var group in serviceGroups)
                {
                    var groupServices = group.ToList();
                    var labels = groupServices.Select(BuildServiceLabel).ToList();

                    int maxLabelWidth = labels
                        .Select(MeasureLabelWidth)
                        .DefaultIfEmpty(0)
                        .Max();

                    int maxNodeWidth = maxLabelWidth + innerPadding * 2;

                    int serviceCount = groupServices.Count;
                    int columns = GetColumnCount(serviceCount);
                    int rows = (int)Math.Ceiling(serviceCount / (double)columns);

                    int groupWidth =
                        swimlanePadding +
                        (columns * maxNodeWidth) +
                        ((columns - 1) * spacing) +
                        swimlanePadding;

                    int groupHeight =
                        swimlanePadding +
                        (rows * serviceNodeHeight) +
                        ((rows - 1) * spacing) +
                        swimlanePadding;

                    string groupId = (idCounter++).ToString();
                    string groupLabel = $"Services: {group.Key}";

                    sb.AppendLine(
                        $"        <mxCell id=\"{groupId}\" value=\"{Escape(groupLabel)}\" style=\"swimlane;rounded=1;fillColor=#d5e8d4;strokeColor=#000000;fontStyle=1\" vertex=\"1\" parent=\"{vmId}\">");
                    sb.AppendLine(
                        $"          <mxGeometry x=\"{swimlanePadding}\" y=\"{serviceGroupY}\" width=\"{groupWidth}\" height=\"{groupHeight}\" as=\"geometry\"/>");
                    sb.AppendLine("        </mxCell>");

                    int index = 0;
                    foreach (var svc in groupServices)
                    {
                        string svcId = (idCounter++).ToString();
                        string svcLabel = BuildServiceLabel(svc);

                        int col = index % columns;
                        int row = index / columns;

                        int x = swimlanePadding + col * (maxNodeWidth + spacing);
                        int y = swimlanePadding + row * (serviceNodeHeight + spacing);

                        sb.AppendLine(
                            $"        <mxCell id=\"{svcId}\" value=\"{Escape(svcLabel)}\" style=\"rounded=1;fillColor=#ffffff;strokeColor=#000000;\" vertex=\"1\" parent=\"{groupId}\">");
                        sb.AppendLine(
                            $"          <mxGeometry x=\"{x}\" y=\"{y}\" width=\"{maxNodeWidth}\" height=\"{serviceNodeHeight}\" as=\"geometry\"/>");
                        sb.AppendLine("        </mxCell>");

                        index++;
                    }

                    serviceGroupY += groupHeight + spacing;
                }

                currentY += vmHeight + spacing;
            }

            // ---------- MOVE TO NEXT GRID CELL (with rowMaxHeight fix) ----------

            column++;

            if (column >= maxColumns)
            {
                column = 0;
                hypervisorX = 40;

                // Move down by tallest item in the row
                hypervisorY += rowMaxHeight + 200;

                // Reset row height
                rowMaxHeight = 0;
            }
            else
            {
                hypervisorX += columnWidth;
            }
        }

        sb.AppendLine("      </root>");
        sb.AppendLine("    </mxGraphModel>");
        sb.AppendLine("  </diagram>");
        sb.AppendLine("</mxfile>");

        return sb.ToString();
    }

    private static int GetColumnCount(int serviceCount)
    {
        if (serviceCount <= 1) return 1;
        if (serviceCount <= 3) return 2;
        return 3;
    }

    private static int MeasureLabelWidth(string label)
    {
        if (string.IsNullOrEmpty(label)) return 0;

        var lines = label.Split('\n');
        int max = 0;

        foreach (var line in lines)
        {
            int width = 0;
            foreach (var ch in line)
            {
                width += ch switch
                {
                    'i' or 'l' or 't' or '1' => 5,
                    'W' or 'M' or '@' or '#' => 9,
                    _ => 7
                };
            }

            if (width > max) max = width;
        }

        return max;
    }

    private static string BuildHypervisorLabel(Hardware h)
    {
        var parts = new List<string> { h.Name };

        if (!string.IsNullOrWhiteSpace(h.Kind))
            parts.Add(h.Kind);

        return string.Join("\n", parts);
    }

    private static string BuildServiceLabel(Service s)
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(s.Name))
            lines.Add(s.Name);

        if (!string.IsNullOrWhiteSpace(s.Network?.Ip))
            lines.Add(s.Network.Ip!);

        if (s.Network?.Port is int port)
        {
            if (!string.IsNullOrWhiteSpace(s.Network.Protocol))
                lines.Add($"{port}/{s.Network.Protocol}");
            else
                lines.Add($"{port}");
        }

        if (!string.IsNullOrWhiteSpace(s.Network?.Url))
            lines.Add(s.Network.Url!);

        return string.Join("\n", lines);
    }

    private static string Escape(string s) =>
        s.Replace("&", "&amp;")
         .Replace("<", "&lt;")
         .Replace(">", "&gt;")
         .Replace("\"", "&quot;")
         .Replace("\n", "&#10;");
}
