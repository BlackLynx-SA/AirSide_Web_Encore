/// <reference path="../../typings/jquery/jquery.d.ts" />
/// <reference path="../../typings/bootstrap.datepicker/bootstrap.datepicker.d.ts" /> 

module AirSide.Components {
    //Enums
    enum PredefinedTypes {
        ThisWeek = 1,
        LastWeek = 2,
        ThisMonth = 3,
        LastMonth = 4,
        ThisYear = 5,
        LastYear = 6
    }
    
    export class CustomDateRangePicker {
        constructor() {
        }

        PredefinedClicked(typeDate: PredefinedTypes) {
            var fromDate: Date = new Date();
            var toDate: Date = new Date();

            switch (typeDate) {
                case PredefinedTypes.ThisWeek:
                    var dt: Date = new Date();
                    var day: number = dt.getDay();
                    var dayDiff: number = 6 - day;
                    fromDate.setDate(dt.getDate() - day);
                    toDate.setDate(dt.getDate() + dayDiff);
                    break;
                case PredefinedTypes.LastWeek:
                    var dt: Date = new Date();
                    dt.setDate(dt.getDate() - 7);
                    var day: number = dt.getDay();
                    var dayDiff: number = 6 - day;
                    fromDate.setDate(dt.getDate() - day);
                    toDate.setDate(dt.getDate() + dayDiff);
                    break;
                case PredefinedTypes.ThisMonth:
                    var dt: Date = new Date();
                    var year: number = dt.getFullYear();
                    var month: number = dt.getMonth();
                    fromDate.setFullYear(year, month, 1);
                    toDate.setFullYear(year, month + 1, 0);
                    break;
                case PredefinedTypes.LastMonth:
                    var dt: Date = new Date();
                    var year: number = dt.getFullYear();
                    var month: number = dt.getMonth() - 1;
                    fromDate.setFullYear(year, month, 1);
                    toDate.setFullYear(year, month + 1, 0);
                    break;
                case PredefinedTypes.ThisYear:
                    var dt: Date = new Date();
                    var year: number = dt.getFullYear();
                    fromDate.setFullYear(year, 0, 1);
                    toDate.setFullYear(year, 11, 31);
                    break;
                case PredefinedTypes.LastYear:
                    var dt: Date = new Date();
                    var year: number = dt.getFullYear() - 1;
                    fromDate.setFullYear(year, 0, 1);
                    toDate.setFullYear(year, 11, 31);
                    break;
                default:
                    break;
            }

            //Year Strings
            var yearFrmStr: string = fromDate.getFullYear().toString();
            var yearToStr: string = toDate.getFullYear().toString();

            //Month Strings
            var monthFrmStr: string = (fromDate.getMonth()+1).toString();
            var monthToStr: string = (toDate.getMonth()+1).toString();

            if (monthFrmStr.length === 1) monthFrmStr = "0" + monthFrmStr;
            if (monthToStr.length === 1) monthToStr = "0" + monthToStr;

            //Day Strings
            var dayFrmStr: string = fromDate.getDate().toString();
            var dayToStr: string = toDate.getDate().toString();

            if (dayFrmStr.length === 1) dayFrmStr = "0" + dayFrmStr;
            if (dayToStr.length === 1) dayToStr = "0" + dayToStr;

            //Set the inputs
            $('.from').val(yearFrmStr + "/" + monthFrmStr + "/" + dayFrmStr);
            $('.to').val(yearToStr + "/" + monthToStr + "/" + dayToStr);
        }
    }
}

var dateRange: AirSide.Components.CustomDateRangePicker = new AirSide.Components.CustomDateRangePicker();

function setDateRange(typeRange: number) {
    dateRange.PredefinedClicked(typeRange);
}