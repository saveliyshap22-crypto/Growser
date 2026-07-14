package com.chromabrowser.app;

public final class PrivateActivity extends MainActivity {
    @Override
    protected boolean isPrivateMode() {
        return true;
    }
}
