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

    Transform _localTransform = null;
    Transform _remoteTransform = null;

    public override void Init()
    {
        SetUpLabel("Point this controller of the current atom...");

        _localController =  SetUpChooser("LocalController",  "Local Controller",  SetLocalController,  SyncLocalControllerChoices);

        SetUpLabel("... towards this other controller:");

        _remoteAtom =       SetUpChooser("RemoteAtom",       "Remote Atom",       null,                SyncRemoteAtomChoices);
        _remoteController = SetUpChooser("RemoteController", "Remote Controller", SetRemoteController, SyncRemoteControllerChoices);

        _angle_offset_x = SetUpFloat("X angle offset", 0f, -180f, 180f);
        _angle_offset_y = SetUpFloat("Y angle offset", 0f, -180f, 180f);
        _angle_offset_z = SetUpFloat("Z angle offset", 0f, -180f, 180f);

        _pauseUpdates = SetUpBool("Pause updates", false);
        SetUpButton("Record current angle offset", SetOffsetToCurrent);
        SetUpButton("Reset offsets to zero",       SetOffsetsToZero);

        //TODO: if offsets have no value, use current offset
        //if (/*detect uninitialized values*/)
        //{
        //    SetOffsetToCurrent();
        //}
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
        _localTransform =
            ControllerByName(containingAtom, uiControl.val).
            transform;
    }

    void SetRemoteController(JSONStorableStringChooser uiControl)
    {
        _remoteTransform =
            ControllerByName(RemoteAtom, uiControl.val).
            transform;
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

    UIDynamicPopup _dp = null;

    JSONStorableStringChooser SetUpChooser(string paramName, string displayName, JSONStorableStringChooser.SetJSONStringCallback setCallback, UIPopup.OnOpenPopup syncCallback)
    {
        var chooser = new JSONStorableStringChooser(paramName, null, null, displayName, setCallback);
        chooser.storeType = JSONStorableParam.StoreType.Full;
        RegisterStringChooser(chooser);

        if (chooser.val != null)
        {
            setCallback(chooser);
        }

        _dp = CreateFilterablePopup(chooser);
        _dp.popup.onOpenPopupHandlers += syncCallback;

        return chooser;
    }

    JSONStorableFloat SetUpFloat(string paramName, float startingValue, float minimum, float maximum)
    {
        var floatJSON = new JSONStorableFloat(paramName, startingValue, minimum, maximum);
        RegisterFloat(floatJSON);
        floatJSON.storeType = JSONStorableParam.StoreType.Full;
        CreateSlider(floatJSON);
        return floatJSON;
    }

    JSONStorableBool SetUpBool(string paramName, bool startingValue)
    {
        var boolJSON = new JSONStorableBool(paramName, startingValue);
        RegisterBool(boolJSON);
        boolJSON.storeType = JSONStorableParam.StoreType.Full;
        CreateToggle(boolJSON);
        return boolJSON;
    }

    void SetUpLabel(string text)
    {
        var tf = CreateTextField(new JSONStorableString("", ""));
        tf.text = text;
        tf.height = 10;
    }

    void SetUpButton(string displayName, UnityEngine.Events.UnityAction callback)
    {
        var button = CreateButton(displayName);
        button.button.onClick.AddListener(callback);
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


