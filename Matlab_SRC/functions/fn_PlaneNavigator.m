function Plane = fn_PlaneNavigator(RTDose, PlaneLabel, PlaneNumber, Figure, cmap)
    
    %Get rid of the singleton dimension from RT dose file using Squeeze
    Volume = RTDose;
    PlaneNumber = round(PlaneNumber);
    
    %Check for the Plane label and align it with the Eclipse winodows
    if strcmp(PlaneLabel, "YZ")
         Plane = squeeze(Volume(:,PlaneNumber, :));
         Plane = imrotate(Plane, 90);
         Plane = fliplr(Plane);
         title = [PlaneLabel, ' - Sagittal'];

    elseif strcmp(PlaneLabel, "XZ")
        Plane = squeeze(Volume(PlaneNumber, :,:));
        Plane = imrotate(Plane, 90);
        title = [PlaneLabel, ' - Coronal'];

    elseif strcmp(PlaneLabel, "XY")
        Plane = squeeze(Volume(:,:,PlaneNumber));  
        title = [PlaneLabel, ' - Axial'];
    end

    %Update the UIAxes 
    if ~isempty(Figure)
        imshow(mat2gray(Plane), [], 'Parent', Figure);
        colormap(Figure, cmap);
        Figure.Title.String = title ;
    end

end
