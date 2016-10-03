var AirSide;
(function (AirSide) {
    var Encore;
    (function (Encore) {
        var Photometric;
        (function (Photometric) {
            var PhotometricReports = (function () {
                function PhotometricReports() {
                    this.photometricData = [];
                    this.dates = [];
                }
                PhotometricReports.prototype.getAvailableDates = function () {
                    var $this = this;
                    $.getJSON("GetAvailableDates", function (data, text, jq) {
                        $this.dates = data;
                        console.log(data);
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
});
