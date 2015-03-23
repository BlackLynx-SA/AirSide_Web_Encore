/// <reference path="../../typings/jquery/jquery.d.ts" />
/// <reference path="../../typings/bootstrap.datepicker/bootstrap.datepicker.d.ts" /> 
var AirSide;
(function (AirSide) {
    var Components;
    (function (Components) {
        //Enums
        var PredefinedTypes;
        (function (PredefinedTypes) {
            PredefinedTypes[PredefinedTypes["ThisWeek"] = 1] = "ThisWeek";
            PredefinedTypes[PredefinedTypes["LastWeek"] = 2] = "LastWeek";
            PredefinedTypes[PredefinedTypes["ThisMonth"] = 3] = "ThisMonth";
            PredefinedTypes[PredefinedTypes["LastMonth"] = 4] = "LastMonth";
            PredefinedTypes[PredefinedTypes["ThisYear"] = 5] = "ThisYear";
            PredefinedTypes[PredefinedTypes["LastYear"] = 6] = "LastYear";
        })(PredefinedTypes || (PredefinedTypes = {}));
        var CustomDateRangePicker = (function () {
            function CustomDateRangePicker() {
            }
            CustomDateRangePicker.prototype.PredefinedClicked = function (typeDate) {
                var fromDate = new Date();
                var toDate = new Date();
                switch (typeDate) {
                    case 1 /* ThisWeek */:
                        var dt = new Date();
                        var day = dt.getDay();
                        var dayDiff = 6 - day;
                        fromDate.setDate(dt.getDate() - day);
                        toDate.setDate(dt.getDate() + dayDiff);
                        break;
                    case 2 /* LastWeek */:
                        var dt = new Date();
                        dt.setDate(dt.getDate() - 7);
                        var day = dt.getDay();
                        var dayDiff = 6 - day;
                        fromDate.setDate(dt.getDate() - day);
                        toDate.setDate(dt.getDate() + dayDiff);
                        break;
                    case 3 /* ThisMonth */:
                        var dt = new Date();
                        var year = dt.getFullYear();
                        var month = dt.getMonth();
                        fromDate.setFullYear(year, month, 1);
                        toDate.setFullYear(year, month + 1, 0);
                        break;
                    case 4 /* LastMonth */:
                        var dt = new Date();
                        var year = dt.getFullYear();
                        var month = dt.getMonth() - 1;
                        fromDate.setFullYear(year, month, 1);
                        toDate.setFullYear(year, month + 1, 0);
                        break;
                    case 5 /* ThisYear */:
                        var dt = new Date();
                        var year = dt.getFullYear();
                        fromDate.setFullYear(year, 0, 1);
                        toDate.setFullYear(year, 11, 31);
                        break;
                    case 6 /* LastYear */:
                        var dt = new Date();
                        var year = dt.getFullYear() - 1;
                        fromDate.setFullYear(year, 0, 1);
                        toDate.setFullYear(year, 11, 31);
                        break;
                    default:
                        break;
                }
                //Year Strings
                var yearFrmStr = fromDate.getFullYear().toString();
                var yearToStr = toDate.getFullYear().toString();
                //Month Strings
                var monthFrmStr = (fromDate.getMonth() + 1).toString();
                var monthToStr = (toDate.getMonth() + 1).toString();
                if (monthFrmStr.length === 1)
                    monthFrmStr = "0" + monthFrmStr;
                if (monthToStr.length === 1)
                    monthToStr = "0" + monthToStr;
                //Day Strings
                var dayFrmStr = fromDate.getDate().toString();
                var dayToStr = toDate.getDate().toString();
                if (dayFrmStr.length === 1)
                    dayFrmStr = "0" + dayFrmStr;
                if (dayToStr.length === 1)
                    dayToStr = "0" + dayToStr;
                console.log(yearFrmStr + "/" + monthFrmStr + "/" + dayFrmStr);
                console.log(yearToStr + "/" + monthToStr + "/" + dayToStr);
            };
            return CustomDateRangePicker;
        })();
        Components.CustomDateRangePicker = CustomDateRangePicker;
    })(Components = AirSide.Components || (AirSide.Components = {}));
})(AirSide || (AirSide = {}));
$(document).on('ready', function (c) {
    var dateRange = new AirSide.Components.CustomDateRangePicker();
    dateRange.PredefinedClicked(1);
    dateRange.PredefinedClicked(2);
    dateRange.PredefinedClicked(3);
    dateRange.PredefinedClicked(4);
    dateRange.PredefinedClicked(5);
    dateRange.PredefinedClicked(6);
});
//# sourceMappingURL=ComponentsHelper.js.map