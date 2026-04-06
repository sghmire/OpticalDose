function fn_UpdateContrast(SLiderUI, UIAxes, Image, contrastValue, ColorMap)

    Image = mat2gray(Image);
    
    low_limit = contrastValue(1); 
    high_limit = contrastValue(2); 

    if high_limit <= low_limit
        return;
    end

    % Clip the image to the selected contrast range
    img_clipped = Image;
    minValue = min(img_clipped(:));
    maxValue = max(img_clipped(:));

    SLiderUI.Limits = [minValue,maxValue ];
    SLiderUI.MajorTicks = linspace(minValue,maxValue , 5);

    img_clipped(img_clipped < low_limit) = low_limit;
    img_clipped(img_clipped > high_limit) = high_limit;

    % Rescale the clipped image to the range [0, 1] for display
    img_rescaled = (img_clipped - low_limit) / (high_limit - low_limit);

    fn_mainImageDisplay(img_rescaled, UIAxes, ColorMap);
end
