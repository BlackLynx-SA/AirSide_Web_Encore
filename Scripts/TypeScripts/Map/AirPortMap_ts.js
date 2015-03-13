/// <reference path="../../typings/jquery/jquery.d.ts" />
var AirSide;
(function (AirSide) {
    var AllAssets = (function () {
        function AllAssets() {
            this.ReceivedAssets = [];
            this.requestAllAssets();
        }
        AllAssets.prototype.requestAllAssets = function () {
            var _this = this;
            $.ajax({
                url: '../../Map/getAllAssets',
                type: 'post',
                dataType: 'json',
                success: function (json) {
                    _this.ReceivedAssets = json;
                    process();
                }
            });
        };
        AllAssets.prototype.findAllInAssetClass = function (assetClassId) {
            var newArray = [];
            $.each(this.ReceivedAssets, function (i, v) {
                if (v.assetClassId === assetClassId)
                    newArray.push(v);
            });
            return newArray;
        };
        AllAssets.prototype.findAllInArea = function (areaId) {
            var newArray = [];
            $.each(this.ReceivedAssets, function (i, v) {
                if (v.location.areaId === areaId)
                    newArray.push(v);
            });
            return newArray;
        };
        AllAssets.prototype.findAllInSubArea = function (subAreaId) {
            var newArray = [];
            $.each(this.ReceivedAssets, function (i, v) {
                if (v.location.areaSubId === subAreaId)
                    newArray.push(v);
            });
            return newArray;
        };
        AllAssets.prototype.findAllFaulty = function (status) {
            var newArray = [];
            $.each(this.ReceivedAssets, function (i, v) {
                if (v.status === status)
                    newArray.push(v);
            });
            return newArray;
        };
        AllAssets.prototype.findWorstCase = function () {
            var newArray = [];
        };
        return AllAssets;
    })();
    var myAssets = new AllAssets();
    function process() {
        var assetClass;
        assetClass = myAssets.findAllInAssetClass(2);
        console.log(assetClass);
    }
})(AirSide || (AirSide = {}));
//# sourceMappingURL=AirPortMap_ts.js.map