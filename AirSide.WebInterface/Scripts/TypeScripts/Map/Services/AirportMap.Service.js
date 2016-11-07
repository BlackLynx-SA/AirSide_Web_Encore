var AirSide;
(function (AirSide) {
    var Encore;
    (function (Encore) {
        var AirportMap;
        (function (AirportMap) {
            var Services = (function () {
                function Services() {
                }
                //-------------------------------------------------------------------------------------
                Services.prototype.getAssets = function () {
                    $.post("../../Map/getAllAssets", function (json) {
                        $(document).trigger('assets.get', [json]);
                    });
                };
                //-------------------------------------------------------------------------------------
                Services.prototype.getSubAreas = function () {
                    $.post("../../Map/getAllSubAreas", function (json) {
                        $(document).trigger('subareas.get', [json]);
                    });
                };
                //-------------------------------------------------------------------------------------
                Services.prototype.getMapCenter = function () {
                    $.post("../../Map/getMapCenter", function (json) {
                        $(document).trigger('mapcenter.get', [json]);
                    });
                };
                //-------------------------------------------------------------------------------------
                Services.prototype.updateFaultyLight = function (assetId, flag) {
                    var data = {
                        assetId: assetId,
                        flag: flag
                    };
                    $.post("../../Map/UpdateFaultyLight", data, function () {
                        $(document).trigger('faultylight.update');
                    });
                };
                //-------------------------------------------------------------------------------------
                Services.prototype.getMultiAssetLocations = function () {
                    $.getJSON("../../Map/GetAllMultiAssets", function (json) {
                        $(document).trigger('multiassetlocation.get', [json]);
                    });
                };
                //-------------------------------------------------------------------------------------
                Services.prototype.getFbTechData = function (date) {
                    var data = {
                        dateForData: date
                    };
                    $.post("../../Map/getFBTechData", data, function (json) {
                        $(document).trigger('fbtech.get', [json]);
                    });
                };
                //-------------------------------------------------------------------------------------
                Services.prototype.getSurveyorData = function (date) {
                    var data = {
                        dateOfSurvey: date
                    };
                    $.post("../../Map/getSurveydData", data, function (json) {
                        $(document).trigger('surveyor.get', [json]);
                    });
                };
                return Services;
            }());
            AirportMap.Services = Services;
        })(AirportMap = Encore.AirportMap || (Encore.AirportMap = {}));
    })(Encore = AirSide.Encore || (AirSide.Encore = {}));
})(AirSide || (AirSide = {}));
//# sourceMappingURL=AirportMap.Service.js.map