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

    Transform _localTransform = null;
    Transform _remoteTransform = null;

    UIDynamicPopup dp = null;

    public override void Init()
    {
        SetUpLabel("Point this controller of the current atom...");

        _localController =  SetUpChooser("LocalController",  "Local Controller",  SetLocalController,  SyncLocalControllerChoices);
        if (_localController.val == null)
        {
            //by default, use first controller in list
            _localController.val = ControllersFor(containingAtom).First().name;
            SetLocalController(_localController);
        }

        SetUpLabel("... toward this controller of this atom:");

        _remoteAtom =       SetUpChooser("RemoteAtom",       "Remote Atom",       null,                SyncRemoteAtomChoices);
        _remoteController = SetUpChooser("RemoteController", "Remote Controller", SetRemoteController, SyncRemoteControllerChoices);
    }

    public void Update()
    {
        if (_localTransform != null && _remoteTransform != null)
        {
            _localTransform.LookAt(_remoteTransform.position);
        }
    }

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

    JSONStorableStringChooser SetUpChooser(string paramName, string displayName, JSONStorableStringChooser.SetJSONStringCallback setCallback, UIPopup.OnOpenPopup syncCallback)
    {
        var chooser = new JSONStorableStringChooser(paramName, null, null, displayName, setCallback);
        RegisterStringChooser(chooser);

        if (chooser.val != null)
        {
            setCallback(chooser);
        }

        dp = CreateFilterablePopup(chooser);
        dp.popup.onOpenPopupHandlers += syncCallback;

        return chooser;
    }

    void SetUpLabel(string text)
    {
        var tf = CreateTextField(new JSONStorableString("", ""));
        tf.text = text;
        tf.height = 10;
    }

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

