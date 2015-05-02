/// <reference path="../../typings/jquery/jquery.d.ts" />
/// <reference path="../../typings/pdfjs/pdfjs.d.ts" />
/// <reference path="../../typings/bootstrap.datepicker/bootstrap.datepicker.d.ts" />

module Reporting {
    
    export class ShiftReport {
        private $reportLoader = $('#reportLoader');
        private $reportImage = $('#reportImage');
        private $pdfDownload = $('#pdfDownload');
        private $excelDownload = $('#excelDownload');

        constructor() {
            //Initialise the date pickers
            this.initDatePickers();
        }

        private initDatePickers() {
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
        }

        generateReport(fromDate: string, toDate: string) {
            this.$reportLoader.fadeIn(300);
            this.$pdfDownload.addClass('disabled');
            this.$excelDownload.addClass('disabled');

            var dateRange: string = fromDate + "-" + toDate;
            var reportPDFUrl: string = 'getShiftsPerDateRangeReport?dateRange=' + dateRange + "&type=2";
            var reportExcelUrl: string = 'getShiftsPerDateRangeReport?dateRange=' + dateRange + "&type=3";
            PDFJS.getDocument(reportPDFUrl).then(pdf=> {
                // Using promise to fetch the page
                pdf.getPage(1).then(page=> {
                    var scale: number = 2;
                    var viewport = page.getViewport(scale);

                    // Prepare canvas using PDF page dimensions
                    var canvas: any = document.getElementById("reportImg");
                    var context = canvas.getContext("2d");
                    canvas.height = viewport.height;
                    canvas.width = viewport.width;

                    // Render PDF page into canvas context
                    var renderContext = {
                        canvasContext: context,
                        viewport: viewport
                    };
                    this.$reportLoader.hide();
                    page.render(renderContext);

                    //Enable download buttons
                    this.$pdfDownload.removeClass('disabled');
                    this.$excelDownload.removeClass('disabled');

                    //Set the URL
                    this.$pdfDownload.attr("href", reportPDFUrl);
                    this.$excelDownload.attr("href", reportExcelUrl);
                });
            })
        }
    }
}

var ShiftReport: Reporting.ShiftReport;

$(document).ready(c=> {
    ShiftReport = new Reporting.ShiftReport();
});

$(document).on('click', '#dateRangeBtn', c => {
    var fromDate: string = $('input.from').val();
    var toDate: string = $('input.to').val();
    
    ShiftReport.generateReport(fromDate, toDate);
});