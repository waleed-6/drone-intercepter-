
# CounterDrone — Full Unity Project (Cloud Build + Colab)

This is a full Unity project ready for **Unity Cloud Build** (no local install required).

## Build Targets
- Windows 64-bit (Mono) → training via Wine in Colab
- Windows 64-bit (IL2CPP) → prettier visual demo (optional)

## Train in Google Colab (Windows build)
```
!apt -y install wine-stable xvfb
!pip install mlagents==0.30.0 torch tensorboard
!unzip -q CounterDrone_Windows.zip -d /content/env
!xvfb-run -a wine64 /content/env/CounterDrone.exe -batchmode -nographics &
!mlagents-learn /content/interceptor_pro.yaml --run-id=intercept-pro --env-args=-logFile,- --env=None
```
Model will be at `/content/results/.../DroneInterceptor.onnx`.
