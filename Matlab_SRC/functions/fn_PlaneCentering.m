   
function CenteredPlane = fn_PlaneCentering(Plane, Pos1, Pos2)    
    [height, width, channels] = size(Plane);
    image_center = [width, height] / 2;
    desired_center = [Pos1,  Pos2];
    translation = ( image_center - desired_center);
 
    tform = rigidtform2d(eye(2), translation);
    followOutput = affineOutputView([height, width], tform, "BoundsStyle", "CenterOutput");
    CenteredPlane = zeros(size(Plane), 'like', Plane);

    for ch = 1:channels
        CenteredPlane(:, :, ch) = imwarp(Plane(:, :, ch), tform, "OutputView", followOutput);
    end
    
end

