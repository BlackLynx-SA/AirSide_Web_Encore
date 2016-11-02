var controller: AirSide.Encore.AirportMap.Controller;

$(document).ready((): void => {
    $('#timepicker').timepicker();

    //-------------------------------------------------------------------------------------

    $(document).on('airportmap.init', (e: Event, c: AirSide.Encore.AirportMap.Controller) => {
        controller = c;
    });

    //-------------------------------------------------------------------------------------

    $(document).on('mapcenter.get', (e: Event, c: Array<number>) => {
        controller.centerLat = c[0];
        controller.centerLong = c[1];
        controller.drawMap();

        //Request all assets
        controller.services.getMultiAssetLocations();
    });

    //-------------------------------------------------------------------------------------

    $(document).on('assets.get', (e: Event, c: Array<IAssetMasterViewModel>) => {
        controller.assets = c;
        controller.filterAssets(true);
        $('#mapLoader').hide();
        $('#mapMenuItems').fadeIn(300);

        //Get All Sub Areas
        controller.services.getSubAreas();
    });

    //-------------------------------------------------------------------------------------

    $(document).on('click', '.show_assets_btn', c => {
        controller.getSelectedAssets();
    });

    //-------------------------------------------------------------------------------------

    $(document).on('subareas.get', (e: Event, c: Array<ISubAreaViewModel>) => {
        controller.subAreas = c;
    });

    //-------------------------------------------------------------------------------------

    $(document).on('fbtech.get', (e: Event, c: Array<IFbTechViewModel>) => {
        controller.fbTechData = c;
        controller.processFbTechData();
    });

    //-------------------------------------------------------------------------------------

    $(document).on('surveyor.get', (e: Event, c: Array<ISurveyorViewModel>) => {
        controller.surveyorData = c;
        controller.processSurveyData();
    });

    //-------------------------------------------------------------------------------------

    $(document).on('multiassetlocation.get', (e: Event, c: Array<IMultiAssetProfileViewModel>) => {
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
        onClose: (selectedDate) => {
            $('input.to').datepicker({
                minDate: selectedDate
            });
        },
        beforeShow: () => {
            setTimeout(() => {
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
        onClose: selectedDate => {
            $('input.from').datepicker({
                maxDate: selectedDate
            });
        },
        beforeShow: () => {
            setTimeout(() => {
                $('.ui-datepicker').css('z-index', 9999);
            }, 0);
        }
    });

    //-------------------------------------------------------------------------------------

    $(document).on('change', '#mainAreas', () => {
        var area = $(this).val();
        controller.filterSubAreas(area);
    });

    //-------------------------------------------------------------------------------------

    $(document).on('click', '#assetFilterBtn', () => {
        controller.filterAssets();
    });

    //-------------------------------------------------------------------------------------

    $(document).on('click', '#shift_create_btn', () => {
        controller.sendCustomShift();
    });

    //-------------------------------------------------------------------------------------

    $(document).on('click', '#areaFilterBtn', () => {
        controller.filterAreas();
    });

    //-------------------------------------------------------------------------------------

    $(document).on('click', '#surveyorFilterBtn', () => {
        controller.requestSurveyorData();
    });

    //-------------------------------------------------------------------------------------

    $(document).on('click', '#photoFilterBtn', () => {
        controller.applyFbTechFilter();
    });

    //-------------------------------------------------------------------------------------

    $(document).on('click', '.assetFilterBtn', () => {
        $('#assetFilterModal').modal('show');
    });

    //-------------------------------------------------------------------------------------

    $(document).on('click', '.areaFilterBtn', () => {
        $('#areaFilterModal').modal('show');
    });

    //-------------------------------------------------------------------------------------

    $(document).on('click', '.surveyorBtn', () => {
        $('#visualSurveyFilterModal').modal('show');
    });

    //-------------------------------------------------------------------------------------

    $(document).on('click', '.photoBtn', () => {
        $('#photometricFilterModal').modal('show');
    });

    //-------------------------------------------------------------------------------------

    $(document).on('click', '.shift_rect_btn', () => {
        controller.showRectangle();
    });

    //-------------------------------------------------------------------------------------

    $(document).on('click', '.faultyLightToggle', e => {
        var item = $(e.target);
        var assetId = '';
        var checked = false;
        if (item.prop('checked')) {
            assetId = item.attr('data-asset-id');
            assetId = assetId.replace('_faultyToggle', '');
            checked = true;
        } else {
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
            success() {
                controller.toggleFaultyLight(assetId, checked);
            }
        });
    });

    //-------------------------------------------------------------------------------------

    $(document).on('click', '#faultyFilterBtn', () => {
        controller.filterFaulty();
    });

    //-------------------------------------------------------------------------------------

    //Select the radio button on drop down select
    $(document).on('click', '#assetClasses', () => {
        $('#assetTypeRadio').prop("checked", null);
        $('#allAssetRadio').prop("checked", null);
        $('#assetTypeRadio').prop("checked", "checked");
    });

    //-------------------------------------------------------------------------------------

    $(document).on('click', '#maintenanceTasks', () => {
        $('#assetTypeRadio').prop("checked", null);
        $('#allAssetRadio').prop("checked", null);
        $('#assetMaintenanceRadio').prop("checked", "checked");
    });

    //-------------------------------------------------------------------------------------

    $(document).on('click', '#assetClasses', () => { });

    //-------------------------------------------------------------------------------------

    $(window).resize(() => { controller.resize() });

    //-------------------------------------------------------------------------------------

    $(document).on('click', '#searchBtn', c => {
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
    } else if (type === 'Roadmap') {
        controller.map.setMapTypeId(google.maps.MapTypeId.ROADMAP);
    } else if (type === 'Hybrid') {
        controller.map.setMapTypeId(google.maps.MapTypeId.HYBRID);
    } else if (type === 'Terrain') {
        controller.map.setMapTypeId(google.maps.MapTypeId.TERRAIN);
    }
}