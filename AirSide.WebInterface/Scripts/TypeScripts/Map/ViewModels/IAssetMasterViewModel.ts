interface IAssetMasterViewModel {
    assetClass: IAssetClass;
    assetClassId: number;
    assetId: number;
    location: ILocationProfile;
    locationId: number;
    maintenance: Array<IMaintenanceProfile>;
    picture: IPictureProfile;
    productUrl: string;
    rfidTag: string;
    serialNumber: string;
    status: boolean;
}

interface IAssetClass {
    assetClassId: number;
    description: string;
    frequencyId: number;
    manufacturer: string;
    model: string;
    pictureId: number;
}

interface ILocationProfile {
    areaId: number;
    areaSubId: number;
    designation: string;
    latitude: number;
    locationId: number;
    longitude: number;
}

interface IMaintenanceProfile {
    maintenanceCycle: number;
    maintenanceId: number;
    maintenanceTask: string;
    nextDate: string;
    previousDate: string;
}

interface IPictureProfile {
    description: string;
    fileLocation: string;
    pictureId: number;
}