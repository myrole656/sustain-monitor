<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Factories\HasFactory;

class Question extends Model
{
    use HasFactory;

    // Mass assignable fields
    protected $fillable = [
        'step_name',
        'enabled',
    ];
}
