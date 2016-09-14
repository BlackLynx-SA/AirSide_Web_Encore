var AirSide;
(function (AirSide) {
    var Encore;
    (function (Encore) {
        var GeoInfo = (function () {
            function GeoInfo() {
                this.multiAssets = [];
                this.init();
            }
            GeoInfo.prototype.init = function () {
                var $this = this;
                $.getJSON("GetAllMultiAssets", function (data, text, jq) {
                    $this.multiAssets = data;
                });
            };
            return GeoInfo;
        }());
        Encore.GeoInfo = GeoInfo;
    })(Encore = AirSide.Encore || (AirSide.Encore = {}));
})(AirSide || (AirSide = {}));
var geoInfo = new AirSide.Encore.GeoInfo();
