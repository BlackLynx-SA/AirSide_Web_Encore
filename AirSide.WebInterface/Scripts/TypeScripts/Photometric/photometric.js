var AirSide;
(function (AirSide) {
    var Encore;
    (function (Encore) {
        var Photometric;
        (function (Photometric) {
            var PhotometricReports = (function () {
                function PhotometricReports() {
                    this.photometricData = [];
                    this.photoDates = [];
                }
                PhotometricReports.prototype.getAvailableDates = function () {
                    var $this = this;
                    $.getJSON("GetAvailableDates", function (data, text, jq) {
                        $this.photoDates = data;
                        $this.buildDateSelect();
                        console.log(data);
                    });
                };
                PhotometricReports.prototype.buildDateSelect = function () {
                    var html = '<option></option>';
                    this.photoDates.forEach(function (c) {
                        html += '<option>' + c + '</option>';
                    });
                    $('#dateSelect').html(html);
                };
                PhotometricReports.prototype.buildChart = function () {
                    Morris.Bar({
                        element: 'bar-chart',
                        data: this.photometricData,
                        xkey: 'SerialNumber',
                        ykeys: ['MaxIntensity', 'AvgIntensity'],
                        labels: ['Max', 'Avg']
                    });
                };
                PhotometricReports.prototype.getPhotometricData = function (date) {
                    var $this = this;
                    var data = { selectedDate: date };
                    $.ajax({
                        url: 'GetPhotometricData',
                        type: 'post',
                        data: data,
                        dataType: 'json',
                        success: function (json) {
                            $this.photometricData = json;
                            $this.buildChart();
                        }
                    });
                };
                return PhotometricReports;
            }());
            Photometric.PhotometricReports = PhotometricReports;
        })(Photometric = Encore.Photometric || (Encore.Photometric = {}));
    })(Encore = AirSide.Encore || (AirSide.Encore = {}));
})(AirSide || (AirSide = {}));
$(document).ready(function () {
    var photometric = new AirSide.Encore.Photometric.PhotometricReports();
    photometric.getAvailableDates();
    $(document).on('change', '#dateSelect', function (c) {
        photometric.getPhotometricData($('#dateSelect').val());
    });
});
