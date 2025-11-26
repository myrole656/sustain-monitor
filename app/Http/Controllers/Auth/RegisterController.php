<?php

namespace App\Http\Controllers\Auth;

use App\Http\Controllers\Controller;
use Illuminate\Http\Request;
use App\Models\User;
use Illuminate\Support\Facades\Hash;

class RegisterController extends Controller
{
    // -------------------------------
    // Show Registration Forms
    // -------------------------------

    /**
     * Show the Admin registration form
     */
    public function showAdminForm()
    {
        return view('auth.register-admin');
    }

    /**
     * Show the User registration form
     */
    public function showUserForm()
    {
        return view('auth.register-user');
    }

    // -------------------------------
    // Handle Registration
    // -------------------------------

    /**
     * Handle Admin registration
     */
    public function registerAdmin(Request $request)
    {
        $request->validate([
            'name'          => 'required|string|max:255',
            'email'         => 'required|email|unique:users,email',
            'password'      => 'required|confirmed|min:6',
            'security_code' => 'required|string',
        ]);

        // ✅ Replace this with your secret admin code
        $adminSecret = env('ADMIN_CODE', 'KDTMS1234');

        if ($request->security_code !== $adminSecret) {
            return back()->withErrors(['security_code' => 'Invalid admin code'])->withInput();
        }

        User::create([
            'name'     => $request->name,
            'email'    => $request->email,
            'role'     => 'admin',
            'password' => Hash::make($request->password),
        ]);

        return redirect()->route('login')->with('success', 'Admin registered successfully!');
    }

    /**
     * Handle User registration
     */
    public function registerUser(Request $request)
    {
        $request->validate([
            'name'     => 'required|string|max:255',
            'email'    => 'required|email|unique:users,email',
            'password' => 'required|confirmed|min:6',
        ]);

        User::create([
            'name'     => $request->name,
            'email'    => $request->email,
            'role'     => 'user',
            'password' => Hash::make($request->password),
        ]);

        return redirect()->route('login')->with('success', 'User registered successfully!');
    }
}
