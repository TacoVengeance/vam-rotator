using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SimpleJSON;

/// <summary>
/// Rotator plugin
/// By TacoVengeance
/// Rotates a controller to constantly point towards another
/// Source: https://github.com/TacoVengeance/vam-rotator
/// </summary>
public class RotatorPlugin : MVRScript
{
    //point this controller of the atom where this plugin resides...
    public JSONStorableStringChooser _localController;

    //...towards this controller of this atom
    public JSONStorableStringChooser _remoteAtom;
    public JSONStorableStringChooser _remoteController;

    public JSONStorableFloat _angle_offset_x;
    public JSONStorableFloat _angle_offset_y;
    public JSONStorableFloat _angle_offset_z;

    public JSONStorableBool _pauseUpdates;
    public UIDynamicButton _setOffsetToCurrent;
    public UIDynamicButton _setOffsetsToZero;

    Transform _localTransform = null;
    Transform _remoteTransform = null;

    static bool UseRightSide = true;

    public override void Init()
    {
        SetUpLabel("Point this controller of the current atom:");

        _localController =  SetUpChooser("LocalController",  "Local Controller",  SetLocalController,  SyncLocalControllerChoices);
        AutoSelectSoleController(containingAtom, _localController);

        SetUpLabel("Towards this other controller:", 2);

        _remoteAtom =       SetUpChooser("RemoteAtom",       "Remote Atom",       SetRemoteAtom,       SyncRemoteAtomChoices);
        _remoteController = SetUpChooser("RemoteController", "Remote Controller", SetRemoteController, SyncRemoteControllerChoices);

        SetUpLabel("Optional angle offsets:", 4);

        _angle_offset_x = SetUpFloat("X angle offset", 0f, -180f, 180f);
        _angle_offset_y = SetUpFloat("Y angle offset", 0f, -180f, 180f);
        _angle_offset_z = SetUpFloat("Z angle offset", 0f, -180f, 180f);

        _setOffsetToCurrent = SetUpButton("Record current angle offset", SetOffsetToCurrent);
        _setOffsetsToZero   = SetUpButton("Reset offsets to zero",       SetOffsetsToZero);

        _pauseUpdates =       SetUpBool("Pause updates", false);
    }

    public void FixedUpdate()
    {
        if (_pauseUpdates.val == false && _localTransform != null && _remoteTransform != null)
        {
            //source: https://forum.unity.com/threads/lookat-with-an-offset.585250/

            _localTransform.rotation =
                Quaternion.LookRotation(_remoteTransform.position - _localTransform.position) *
                Quaternion.Euler(_angle_offset_x.val, _angle_offset_y.val, _angle_offset_z.val);
        }
    }

    #region UI

    void SetLocalController(JSONStorableStringChooser uiControl)
    {
        _localTransform = ControllerByName(containingAtom, uiControl.val).transform;
    }

    void SetRemoteAtom(JSONStorableStringChooser uiControl)
    {
        AutoSelectSoleController(RemoteAtom, _remoteController);
    }

    void SetRemoteController(JSONStorableStringChooser uiControl)
    {
        _remoteTransform = ControllerByName(RemoteAtom, uiControl.val).transform;
    }

    void SyncLocalControllerChoices()
    {
        _localController.choices = ControllersFor(containingAtom).Select(fc => fc.name).ToList();
    }

    void SyncRemoteControllerChoices()
    {
        if (_remoteAtom.val != null)
        {
            _remoteController.choices = ControllersFor(RemoteAtom).Select(fc => fc.name).ToList();
        }
    }

    void SyncRemoteAtomChoices()
    {
        _remoteAtom.choices = SuperController.singleton.GetAtoms().Select(fc => fc.name).ToList();
    }

    void SetUpLabel(string text, int rows = 1)
    {
        var tf = CreateTextField(new JSONStorableString("", ""));
        tf.text = "\n" + text;
        tf.height = 120f + (rows - 1) * 135f;
    }

    JSONStorableStringChooser SetUpChooser(string paramName, string displayName, JSONStorableStringChooser.SetJSONStringCallback setCallback, UIPopup.OnOpenPopup syncCallback)
    {
        var chooser = new JSONStorableStringChooser(paramName, null, null, displayName, setCallback);
        chooser.storeType = JSONStorableParam.StoreType.Full;
        RegisterStringChooser(chooser);

        if (chooser.val != null)
        {
            setCallback(chooser);
        }

        var _dp = CreateFilterablePopup(chooser, UseRightSide);
        _dp.popup.onOpenPopupHandlers += syncCallback;

        return chooser;
    }

    JSONStorableFloat SetUpFloat(string paramName, float startingValue, float minimum, float maximum)
    {
        var floatJSON = new JSONStorableFloat(paramName, startingValue, minimum, maximum);
        RegisterFloat(floatJSON);
        floatJSON.storeType = JSONStorableParam.StoreType.Full;
        CreateSlider(floatJSON, UseRightSide);
        return floatJSON;
    }

    JSONStorableBool SetUpBool(string paramName, bool startingValue)
    {
        var boolJSON = new JSONStorableBool(paramName, startingValue);
        RegisterBool(boolJSON);
        boolJSON.storeType = JSONStorableParam.StoreType.Full;
        CreateToggle(boolJSON, UseRightSide);
        return boolJSON;
    }

    UIDynamicButton SetUpButton(string displayName, UnityEngine.Events.UnityAction callback)
    {
        var button = CreateButton(displayName, UseRightSide);
        button.button.onClick.AddListener(callback);
        return button;
    }

    void SetOffsetsToZero()
    {
        _angle_offset_x.val = 0f;
        _angle_offset_y.val = 0f;
        _angle_offset_z.val = 0f;
    }

    void SetOffsetToCurrent()
    {
        var rotation = Quaternion.Inverse(Quaternion.LookRotation(_remoteTransform.position - _localTransform.position)) * _localTransform.rotation;

        _angle_offset_x.val = ParseAngle(rotation.eulerAngles.x);
        _angle_offset_y.val = ParseAngle(rotation.eulerAngles.y);
        _angle_offset_z.val = ParseAngle(rotation.eulerAngles.z);
    }

    float ParseAngle(float angle)
    {
        return angle > 180 ? angle - 360 : angle;
    }

    void AutoSelectSoleController(Atom atom, JSONStorableStringChooser controllerChooser)
    {
        //if atom only has one controller, then select it
        var controllers = ControllersFor(atom);
        if (controllers.Count == 1)
        {
            controllerChooser.val = controllers.First().name;
        }
    }

    #endregion

    List<FreeControllerV3> ControllersFor(Atom atom)
    {
        return atom.freeControllers.ToList();
    }

    FreeControllerV3 ControllerByName(Atom atom, string name)
    {
        return ControllersFor(atom).Find(fc => fc.name == name);
    }

    Atom RemoteAtom
    {
        get { return SuperController.singleton.GetAtomByUid(_remoteAtom.val); }
    }
}

