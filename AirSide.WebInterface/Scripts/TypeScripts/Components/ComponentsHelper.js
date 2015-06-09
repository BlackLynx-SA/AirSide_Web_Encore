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
                    case PredefinedTypes.ThisWeek:
                        var dt = new Date();
                        var day = dt.getDay();
                        var dayDiff = 6 - day;
                        fromDate.setDate(dt.getDate() - day);
                        toDate.setDate(dt.getDate() + dayDiff);
                        break;
                    case PredefinedTypes.LastWeek:
                        var dt = new Date();
                        dt.setDate(dt.getDate() - 7);
                        var day = dt.getDay();
                        var dayDiff = 6 - day;
                        fromDate.setDate(dt.getDate() - day);
                        toDate.setDate(dt.getDate() + dayDiff);
                        break;
                    case PredefinedTypes.ThisMonth:
                        var dt = new Date();
                        var year = dt.getFullYear();
                        var month = dt.getMonth();
                        fromDate.setFullYear(year, month, 1);
                        toDate.setFullYear(year, month + 1, 0);
                        break;
                    case PredefinedTypes.LastMonth:
                        var dt = new Date();
                        var year = dt.getFullYear();
                        var month = dt.getMonth() - 1;
                        fromDate.setFullYear(year, month, 1);
                        toDate.setFullYear(year, month + 1, 0);
                        break;
                    case PredefinedTypes.ThisYear:
                        var dt = new Date();
                        var year = dt.getFullYear();
                        fromDate.setFullYear(year, 0, 1);
                        toDate.setFullYear(year, 11, 31);
                        break;
                    case PredefinedTypes.LastYear:
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
                //Set the inputs
                $('.from').val(yearFrmStr + "/" + monthFrmStr + "/" + dayFrmStr);
                $('.to').val(yearToStr + "/" + monthToStr + "/" + dayToStr);
            };
            return CustomDateRangePicker;
        })();
        Components.CustomDateRangePicker = CustomDateRangePicker;
    })(Components = AirSide.Components || (AirSide.Components = {}));
})(AirSide || (AirSide = {}));
var dateRange = new AirSide.Components.CustomDateRangePicker();
function setDateRange(typeRange) {
    dateRange.PredefinedClicked(typeRange);
}
//# sourceMappingURL=ComponentsHelper.js.map