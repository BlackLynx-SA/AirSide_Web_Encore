var controller;
$(document).ready(function () {
    //Remove Worscase from Shift Creation
    $("#shiftTasks option[value='0']").each(function () {
        $(this).remove();
    });
    //-------------------------------------------------------------------------------------
    $('#timepicker').timepicker();
    //-------------------------------------------------------------------------------------
    $(document).on('airportmap.init', function (e, c) {
        controller = c;
    });
    //-------------------------------------------------------------------------------------
    $(document).on('mapcenter.get', function (e, c) {
        controller.centerLat = c[0];
        controller.centerLong = c[1];
        controller.drawMap();
        //Request all assets
        controller.services.getMultiAssetLocations();
    });
    //-------------------------------------------------------------------------------------
    $(document).on('assets.get', function (e, c) {
        controller.assets = c;
        controller.filterAssets(true);
        $('#mapLoader').hide();
        $('#mapMenuItems').fadeIn(300);
        //Get All Sub Areas
        controller.services.getSubAreas();
    });
    //-------------------------------------------------------------------------------------
    $(document).on('click', '.show_assets_btn', function (c) {
        controller.getSelectedAssets();
    });
    //-------------------------------------------------------------------------------------
    $(document).on('subareas.get', function (e, c) {
        controller.subAreas = c;
    });
    //-------------------------------------------------------------------------------------
    $(document).on('fbtech.get', function (e, c) {
        controller.fbTechData = c;
        controller.processFbTechData();
    });
    //-------------------------------------------------------------------------------------
    $(document).on('surveyor.get', function (e, c) {
        controller.surveyorData = c;
        controller.processSurveyData();
    });
    //-------------------------------------------------------------------------------------
    $(document).on('multiassetlocation.get', function (e, c) {
        controller.multiAssets = c;
        //Request All Assets
        controller.services.getAssets();
    });
    //-------------------------------------------------------------------------------------
    //Init Date Range
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
        },
        beforeShow: function () {
            setTimeout(function () {
                $('.ui-datepicker').css('z-index', 9999);
            }, 0);
        }
    });
    //-------------------------------------------------------------------------------------
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
        },
        beforeShow: function () {
            setTimeout(function () {
                $('.ui-datepicker').css('z-index', 9999);
            }, 0);
        }
    });
    //-------------------------------------------------------------------------------------
    $(document).on('change', '#mainAreas', function () {
        var area = $('#mainAreas').val();
        controller.filterSubAreas(area);
    });
    //-------------------------------------------------------------------------------------
    $(document).on('click', '#assetFilterBtn', function () {
        controller.filterAssets();
    });
    //-------------------------------------------------------------------------------------
    $(document).on('click', '#shift_create_btn', function () {
        controller.sendCustomShift();
    });
    //-------------------------------------------------------------------------------------
    $(document).on('click', '#areaFilterBtn', function () {
        controller.filterAreas();
    });
    //-------------------------------------------------------------------------------------
    $(document).on('click', '#surveyorFilterBtn', function () {
        controller.requestSurveyorData();
    });
    //-------------------------------------------------------------------------------------
    $(document).on('click', '#photoFilterBtn', function () {
        controller.applyFbTechFilter();
    });
    //-------------------------------------------------------------------------------------
    $(document).on('click', '.assetFilterBtn', function () {
        $('#assetFilterModal').modal('show');
    });
    //-------------------------------------------------------------------------------------
    $(document).on('click', '.areaFilterBtn', function () {
        $('#areaFilterModal').modal('show');
    });
    //-------------------------------------------------------------------------------------
    $(document).on('click', '.surveyorBtn', function () {
        $('#visualSurveyFilterModal').modal('show');
    });
    //-------------------------------------------------------------------------------------
    $(document).on('click', '.photoBtn', function () {
        $('#photometricFilterModal').modal('show');
    });
    //-------------------------------------------------------------------------------------
    $(document).on('click', '.shift_rect_btn', function () {
        controller.showRectangle();
    });
    //-------------------------------------------------------------------------------------
    $(document).on('click', '.faultyLightToggle', function (e) {
        var item = $(e.target);
        var assetId = '';
        var checked = false;
        if (item.prop('checked')) {
            assetId = item.attr('data-asset-id');
            assetId = assetId.replace('_faultyToggle', '');
            checked = true;
        }
        else {
            assetId = item.attr('data-asset-id');
            assetId = assetId.replace('_faultyToggle', '');
            checked = false;
        }
        var data = {
            assetId: assetId,
            flag: checked
        };
        $.ajax({
            url: '../../Map/UpdateFaultyLight',
            type: 'post',
            data: data,
            dataType: 'json',
            success: function () {
                controller.toggleFaultyLight(assetId, checked);
            }
        });
    });
    //-------------------------------------------------------------------------------------
    $(document).on('click', '#faultyFilterBtn', function () {
        controller.filterFaulty();
    });
    //-------------------------------------------------------------------------------------
    //Select the radio button on drop down select
    $(document).on('click', '#assetClasses', function () {
        $('#assetTypeRadio').prop("checked", null);
        $('#allAssetRadio').prop("checked", null);
        $('#assetTypeRadio').prop("checked", "checked");
    });
    //-------------------------------------------------------------------------------------
    $(document).on('click', '#maintenanceTasks', function () {
        $('#assetTypeRadio').prop("checked", null);
        $('#allAssetRadio').prop("checked", null);
        $('#assetMaintenanceRadio').prop("checked", "checked");
    });
    //-------------------------------------------------------------------------------------
    $(document).on('click', '#assetClasses', function () { });
    //-------------------------------------------------------------------------------------
    $(window).resize(function () { controller.resize(); });
    //-------------------------------------------------------------------------------------
    $(document).on('click', '#searchBtn', function (c) {
        controller.applySearchFilter();
    });
    //-------------------------------------------------------------------------------------
});
function setTask(id, desc) {
    var $this = controller;
    $this.selectedTask = id;
    $('#taskLabel').html(desc);
    if ($this.lastFilter === "assets")
        $this.filterAssets();
    else if ($this.lastFilter === "areas")
        $this.filterAreas();
}
function changeMap(type) {
    if (type === 'Satellite') {
        controller.map.setMapTypeId(google.maps.MapTypeId.SATELLITE);
    }
    else if (type === 'Roadmap') {
        controller.map.setMapTypeId(google.maps.MapTypeId.ROADMAP);
    }
    else if (type === 'Hybrid') {
        controller.map.setMapTypeId(google.maps.MapTypeId.HYBRID);
    }
    else if (type === 'Terrain') {
        controller.map.setMapTypeId(google.maps.MapTypeId.TERRAIN);
    }
}
//# sourceMappingURL=AirportMap.js.map