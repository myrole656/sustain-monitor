<?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;
use Symfony\Component\HttpFoundation\Response;

class GuidelineController extends Controller
{
    // Preview inside iframe
    public function preview()
    {
        $path = public_path('pdf/guideline.pdf');

        if (!file_exists($path)) {
            abort(404, 'PDF not found.');
        }

        return response()->file($path, [
            'Content-Type' => 'application/pdf',
            'Content-Disposition' => 'inline; filename="guideline.pdf"',
        ]);
    }

    // Download file
    public function download()
    {
        $path = public_path('pdf/guideline.pdf');

        if (!file_exists($path)) {
            abort(404, 'PDF not found.');
        }

        return response()->download($path, 'guideline.pdf');
    }
}
