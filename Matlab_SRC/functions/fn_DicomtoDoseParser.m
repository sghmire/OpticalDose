function [dicomplane, pixelspacing] = fn_DicomtoDoseParser(dicomfile)
    info = dicominfo(dicomfile);

    plane = double(squeeze(dicomread(info)));
    dosescaling = info.DoseGridScaling;
    dicomplane = plane * dosescaling * 10;   

    pixelspacing = info.PixelSpacing;
end