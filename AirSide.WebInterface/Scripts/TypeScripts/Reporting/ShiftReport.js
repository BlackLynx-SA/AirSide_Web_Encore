/// <reference path="../../typings/jquery/jquery.d.ts" />
/// <reference path="../../typings/pdfjs/pdfjs.d.ts" />
/// <reference path="../../typings/bootstrap.datepicker/bootstrap.datepicker.d.ts" />
var Reporting;
(function (Reporting) {
    var ShiftReport = (function () {
        function ShiftReport() {
            this.$reportLoader = $('#reportLoader');
            this.$reportImage = $('#reportImage');
            this.$pdfDownload = $('#pdfDownload');
            this.$excelDownload = $('#excelDownload');
            //Initialise the date pickers
            this.initDatePickers();
        }
        ShiftReport.prototype.initDatePickers = function () {
            // Date Range Picker
            $("input.from").datepicker({
                changeMonth: true,
                numberOfMonths: 3,
                dateFormat: 'yy/mm/dd',
                prevText: '<i class="fa fa-chevron-left"></i>',
                nextText: '<i class="fa fa-chevron-right"></i>',
                onClose: function (selectedDate) {
                    $('input.to').datepicker({
                        minDate: selectedDate
                    });
                }
            });
            $("input.to").datepicker({
                defaultDate: "+1w",
                changeMonth: true,
                numberOfMonths: 3,
                dateFormat: 'yy/mm/dd',
                prevText: '<i class="fa fa-chevron-left"></i>',
                nextText: '<i class="fa fa-chevron-right"></i>',
                onClose: function (selectedDate) {
                    $('input.from').datepicker({
                        maxDate: selectedDate
                    });
                }
            });
        };
        ShiftReport.prototype.generateReport = function (fromDate, toDate) {
            var _this = this;
            this.$reportLoader.fadeIn(300);
            this.$pdfDownload.addClass('disabled');
            this.$excelDownload.addClass('disabled');
            var dateRange = fromDate + "-" + toDate;
            var reportPDFUrl = 'getShiftsPerDateRangeReport?dateRange=' + dateRange + "&type=2";
            var reportExcelUrl = 'getShiftsPerDateRangeReport?dateRange=' + dateRange + "&type=3";
            PDFJS.getDocument(reportPDFUrl).then(function (pdf) {
                // Using promise to fetch the page
                pdf.getPage(1).then(function (page) {
                    var scale = 2;
                    var viewport = page.getViewport(scale);
                    // Prepare canvas using PDF page dimensions
                    var canvas = document.getElementById("reportImg");
                    var context = canvas.getContext("2d");
                    canvas.height = viewport.height;
                    canvas.width = viewport.width;
                    // Render PDF page into canvas context
                    var renderContext = {
                        canvasContext: context,
                        viewport: viewport
                    };
                    _this.$reportLoader.hide();
                    page.render(renderContext);
                    //Enable download buttons
                    _this.$pdfDownload.removeClass('disabled');
                    _this.$excelDownload.removeClass('disabled');
                    //Set the URL
                    _this.$pdfDownload.attr("href", reportPDFUrl);
                    _this.$excelDownload.attr("href", reportExcelUrl);
                });
            });
        };
        return ShiftReport;
    })();
    Reporting.ShiftReport = ShiftReport;
})(Reporting || (Reporting = {}));
var ShiftReport;
$(document).ready(function (c) {
    ShiftReport = new Reporting.ShiftReport();
});
$(document).on('click', '#dateRangeBtn', function (c) {
    var fromDate = $('input.from').val();
    var toDate = $('input.to').val();
    ShiftReport.generateReport(fromDate, toDate);
});
//# sourceMappingURL=ShiftReport.js.map