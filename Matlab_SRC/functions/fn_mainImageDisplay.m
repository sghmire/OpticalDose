function fn_mainImageDisplay(film, UIAxes, cmap, DPI, CenterPoint)
    % Display the image
    film = mat2gray(film);
    imshow(film, [], 'Parent', UIAxes);
    axis(UIAxes, 'image');
    colormap(UIAxes, cmap);
    
    [rows, cols, ~] = size(film);
    
    % If DPI is provided, we can show Millimeter Axes
    % If CenterPoint is missing/empty, default to [1, 1] (Upper-Left Corner Origin)
    if nargin >= 4 && ~isempty(DPI)
        if nargin < 5 || isempty(CenterPoint)
            CenterPoint = [1, 1];
        end
        
        mmPerPixel = 25.4 / DPI;
        
        % Calculate limits in mm 
        % (CenterPoint(1) is x_px, CenterPoint(2) is y_px)
        xLimitsPx = [1, cols];
        xLimitsMm = (xLimitsPx - CenterPoint(1)) * mmPerPixel;
        
        yLimitsPx = [1, rows];
        yLimitsMm = (yLimitsPx - CenterPoint(2)) * mmPerPixel;
        
        % Determine a reasonable step (e.g., 10mm, 20mm, 50mm)
        maxDim = max(max(abs(xLimitsMm)), max(abs(yLimitsMm)));
        if maxDim < 50
            stepMm = 10;
        elseif maxDim < 150
            stepMm = 20;
        else
            stepMm = 50;
        end
        
        % Generate Ticks in mm
        xTicksMm = floor(xLimitsMm(1)/stepMm)*stepMm : stepMm : ceil(xLimitsMm(2)/stepMm)*stepMm;
        yTicksMm = floor(yLimitsMm(1)/stepMm)*stepMm : stepMm : ceil(yLimitsMm(2)/stepMm)*stepMm;
        
        % Convert back to pixels for axis placement
        UIAxes.XTick = (xTicksMm / mmPerPixel) + CenterPoint(1);
        UIAxes.YTick = (yTicksMm / mmPerPixel) + CenterPoint(2);
        
        % Label the ticks in mm
        UIAxes.XTickLabel = string(xTicksMm);
        UIAxes.YTickLabel = string(yTicksMm);
        
        xlabel(UIAxes, 'X (mm)');
        ylabel(UIAxes, 'Y (mm)');
    else
        % Fallback to Pixel Axes
        tick_ct = max(rows, cols);
        tick_ct = round(tick_ct / 12);

        UIAxes.XTick = 0:tick_ct:cols;
        UIAxes.YTick = 0:tick_ct:rows;
        UIAxes.XTickLabel = string(UIAxes.XTick);
        UIAxes.YTickLabel = string(UIAxes.YTick);
        
        xlabel(UIAxes, 'X (pixels)');
        ylabel(UIAxes, 'Y (pixels)');
    end
    
    % Common axis formatting
    UIAxes.XTickMode = 'manual';
    UIAxes.YTickMode = 'manual';
    UIAxes.Visible = 'on';
end